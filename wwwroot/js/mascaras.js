// Máscara de telefone celular brasileiro: (00) 00000-0000
function aplicarMascaraTelefone(elemento) {
    elemento.addEventListener('input', function (e) {
        let valor = e.target.value.replace(/\D/g, ''); // Remove tudo que não é dígito
        
        if (valor.length > 11) {
            valor = valor.slice(0, 11);
        }
        
        if (valor.length <= 10) {
            // Telefone fixo: (00) 0000-0000
            valor = valor.replace(/^(\d{2})(\d{4})(\d{0,4}).*/, '($1) $2-$3');
        } else {
            // Celular: (00) 00000-0000
            valor = valor.replace(/^(\d{2})(\d{5})(\d{0,4}).*/, '($1) $2-$3');
        }
        
        e.target.value = valor;
    });
}

// Aplicar máscara em todos os inputs de telefone ao carregar a página
document.addEventListener('DOMContentLoaded', function() {
    // Seleciona todos os inputs com name ou id contendo 'telefone' (case insensitive)
    const inputsTelefone = document.querySelectorAll('input[name*="elefone" i], input[id*="elefone" i], input[type="tel"]');
    
    inputsTelefone.forEach(function(input) {
        aplicarMascaraTelefone(input);
    });
});
