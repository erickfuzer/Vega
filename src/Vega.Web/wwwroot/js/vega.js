window.vega = {
    baixar: function (nome, bytes) {
        var blob = new Blob([bytes], { type: 'application/octet-stream' });
        var url = URL.createObjectURL(blob);
        var link = document.createElement('a');
        link.href = url;
        link.download = nome;
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
    },

    copiar: function (texto) {
        return navigator.clipboard.writeText(texto).then(function () { return true; }, function () { return false; });
    },

    alternarTema: function () {
        var raiz = document.documentElement;
        var novo = raiz.dataset.theme === 'claro' ? 'grafite' : 'claro';
        raiz.dataset.theme = novo;
        try { localStorage.setItem('vega-tema', novo); } catch (e) { }
        return novo;
    },

    temaAtual: function () {
        return document.documentElement.dataset.theme;
    },

    abrirDialogo: function (id) {
        var dialogo = document.getElementById(id);
        if (dialogo && !dialogo.open) dialogo.showModal();
    },

    fecharDialogo: function (id) {
        var dialogo = document.getElementById(id);
        if (dialogo) dialogo.close();
    },

    mostrar: function (seletor) {
        var elemento = document.querySelector(seletor);
        if (elemento) elemento.scrollIntoView({ block: 'center' });
    }
};
