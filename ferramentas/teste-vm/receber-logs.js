// Recebe o zip que o coleta.ps1 envia de dentro da VM, sem precisar de administrador.
// Na rede NAT do VirtualBox, a VM enxerga o 127.0.0.1 deste PC no endereço 10.0.2.2.
// Uso: node receber-logs.js <pasta>   (escuta só em 127.0.0.1:5299)
const http = require('http');
const fs = require('fs');
const path = require('path');

const pasta = path.resolve(process.argv[2] || path.join(__dirname, 'recebidos'));
const limite = 500 * 1024 * 1024;
fs.mkdirSync(pasta, { recursive: true });

http.createServer((req, res) => {
    if (req.method !== 'POST') { res.writeHead(405); res.end(); return; }
    const destino = path.join(pasta, `coleta-${new Date().toISOString().replace(/[:.]/g, '-')}.zip`);
    const arquivo = fs.createWriteStream(destino);
    let tamanho = 0;
    req.on('data', parte => {
        tamanho += parte.length;
        if (tamanho > limite) { req.destroy(); arquivo.destroy(); fs.rmSync(destino, { force: true }); }
    });
    req.pipe(arquivo);
    arquivo.on('finish', () => {
        console.log(`recebido: ${destino} (${tamanho} bytes)`);
        res.writeHead(200, { 'Content-Type': 'text/plain; charset=utf-8' });
        res.end('ok');
    });
}).listen(5299, '127.0.0.1', () => console.log(`esperando em 127.0.0.1:5299, salvando em ${pasta}`));
