param([Parameter(Mandatory)][string[]] $Arquivos)

$ns = @{ u = 'urn:schemas-microsoft-com:unattend'; s = 'https://schneegans.de/windows/unattend-generator/' }

foreach ($arquivo in $Arquivos) {
    "=== $(Split-Path $arquivo -Leaf)"
    $xml = [xml]::new()
    $xml.Load($arquivo)
    $problemas = 0

    # 1. Sintaxe de cada script PowerShell embutido
    $scripts = @()
    foreach ($f in (Select-Xml -Xml $xml -Namespace $ns -XPath '//s:File').Node) {
        $scripts += [pscustomobject]@{ Nome = $f.path; Texto = $f.InnerText }
    }
    $extract = (Select-Xml -Xml $xml -Namespace $ns -XPath '//s:ExtractScript').Node
    if ($extract) { $scripts += [pscustomobject]@{ Nome = 'ExtractScript'; Texto = $extract.InnerText } }
    foreach ($s in $scripts | Where-Object { $_.Nome -like '*.ps1' -or $_.Nome -eq 'ExtractScript' }) {
        $erros = $null
        [void][System.Management.Automation.Language.Parser]::ParseInput($s.Texto, [ref]$null, [ref]$erros)
        if ($erros) { $problemas++; "  SINTAXE $($s.Nome): $($erros[0].Message) (linha $($erros[0].Extent.StartLineNumber))" }
    }
    "  scripts PowerShell conferidos: $(($scripts | Where-Object { $_.Nome -like '*.ps1' -or $_.Nome -eq 'ExtractScript' }).Count)"

    # 2. XML embutido (tarefas, layout) precisa ser XML válido
    foreach ($s in $scripts | Where-Object { $_.Nome -like '*.xml' }) {
        try { [void]([xml]$s.Texto) } catch { $problemas++; "  XML INVÁLIDO $($s.Nome): $($_.Exception.Message)" }
    }

    # 3. Limite de 259 caracteres em cada Path de RunSynchronous
    $paths = (Select-Xml -Xml $xml -Namespace $ns -XPath '//u:RunSynchronousCommand/u:Path').Node
    $maior = ($paths | ForEach-Object { $_.InnerText.Length } | Measure-Object -Maximum).Maximum
    foreach ($p in $paths | Where-Object { $_.InnerText.Length -gt 259 }) { $problemas++; "  PATH LONGO ($($p.InnerText.Length)): $($p.InnerText.Substring(0, 60))..." }
    "  comandos RunSynchronous: $($paths.Count), maior com $maior caracteres"
    $linhas = (Select-Xml -Xml $xml -Namespace $ns -XPath '//u:SynchronousCommand/u:CommandLine').Node
    foreach ($l in $linhas | Where-Object { $_.InnerText.Length -gt 1024 }) { $problemas++; "  COMANDO DE LOGIN LONGO ($($l.InnerText.Length))" }

    # 4. Aprendizados das instalações reais
    $pe = (Select-Xml -Xml $xml -Namespace $ns -XPath "//u:settings[@pass='windowsPE']//u:Path").Node | ForEach-Object InnerText
    if ($pe -match 'findstr') { $problemas++; '  WINPE usa findstr, que não existe na instalação' }
    if ($xml.OuterXml -match 'CloudExperienceHost\\CreateObjectTask') { $problemas++; '  desativa CreateObjectTask, que quebra a OOBE' }
    foreach ($s in $scripts) { if ($s.Texto -match '@\(\s*@\(') { "  AVISO $($s.Nome): lista dentro de lista, conferir se não achata" } }

    # 5. Script do WinPE: rótulos repetidos e goto sem destino
    $copia = (Select-Xml -Xml $xml -Namespace $ns -XPath '//s:PEScriptCopy').Node
    if ($copia) {
        $texto = $copia.InnerText
        $rotulos = [regex]::Matches($texto, '(?m)^[ \t]*:(\w+)') | ForEach-Object { $_.Groups[1].Value.ToLower() }
        $repetidos = $rotulos | Group-Object | Where-Object Count -gt 1 | ForEach-Object Name
        if ($repetidos) { $problemas++; "  PE.CMD rótulos repetidos: $($repetidos -join ', ')" }
        $destinos = [regex]::Matches($texto, '(?i)goto\s+:?(\w+)') | ForEach-Object { $_.Groups[1].Value.ToLower() } | Where-Object { $_ -ne 'eof' } | Sort-Object -Unique
        $faltando = $destinos | Where-Object { $_ -notin $rotulos }
        if ($faltando) { $problemas++; "  PE.CMD goto sem rótulo: $($faltando -join ', ')" }
        $echo = @($pe | Where-Object { $_ -match '>>"?X:\\pe\.cmd' }).Count
        "  pe.cmd: $(($texto -split "`n").Count) linhas na cópia, $echo linhas de echo, rótulos: $(($rotulos | Sort-Object -Unique) -join ', ')"

        # 6. Remonta o pe.cmd com as regras de escape do cmd, sem executar nada
        $comandos = (Select-Xml -Xml $xml -Namespace $ns -XPath "//u:settings[@pass='windowsPE']//u:RunSynchronousCommand").Node |
            Sort-Object { [int]$_.Order } | ForEach-Object { $_.Path } | Where-Object { $_ -like 'cmd.exe /c >>X:\pe.cmd (*' }
        $remontado = [System.Collections.Generic.List[string]]::new()
        foreach ($c in $comandos) {
            $corpo = $c.Substring('cmd.exe /c >>X:\pe.cmd ('.Length)
            $partes = [System.Collections.Generic.List[string]]::new()
            $atual = [System.Text.StringBuilder]::new()
            $aspas = $false; $fechou = $false; $i = 0
            while ($i -lt $corpo.Length) {
                $ch = $corpo[$i]
                if ($aspas) { [void]$atual.Append($ch); if ($ch -eq '"') { $aspas = $false } }
                elseif ($ch -eq '^') { $i++; if ($i -lt $corpo.Length) { [void]$atual.Append($corpo[$i]) } }
                elseif ($ch -eq '"') { [void]$atual.Append($ch); $aspas = $true }
                elseif ($ch -eq '&') { $partes.Add($atual.ToString()); [void]$atual.Clear() }
                elseif ($ch -eq ')') { $fechou = $true; if ($i -ne $corpo.Length - 1) { $problemas++; "  PE.CMD ')' sem escape no meio da linha: $c" }; break }
                else { [void]$atual.Append($ch) }
                $i++
            }
            if (-not $fechou) { $problemas++; "  PE.CMD linha sem ')' final: $c" }
            $partes.Add($atual.ToString())
            foreach ($p in $partes) {
                if (-not $p.StartsWith('echo:')) { $problemas++; "  PE.CMD EXECUTARIA FORA DO ECHO: $p" } else { $remontado.Add($p.Substring(5)) }
            }
        }
        $esperado = @($texto -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' })
        $remontado = @($remontado | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' })
        $diferencas = 0
        for ($k = 0; $k -lt [math]::Max($esperado.Count, $remontado.Count); $k++) {
            $a = if ($k -lt $esperado.Count) { $esperado[$k].TrimEnd() } else { '<nada>' }
            $b = if ($k -lt $remontado.Count) { $remontado[$k].TrimEnd() } else { '<nada>' }
            if ($a -ne $b) { $diferencas++; if ($diferencas -le 5) { "  PE.CMD linha $($k + 1) difere:`n    cópia:     $a`n    remontado: $b" } }
        }
        if ($diferencas) { $problemas++ }
        "  pe.cmd remontado: $($remontado.Count) linhas, $diferencas diferentes da cópia"
    }

    "  problemas: $problemas"
}
