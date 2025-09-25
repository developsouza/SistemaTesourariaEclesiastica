// Validações em tempo real para o sistema
document.addEventListener('DOMContentLoaded', function() {
    
    // Validação de CPF em tempo real
    const cpfInputs = document.querySelectorAll('input[data-val-cpf]');
    cpfInputs.forEach(input => {
        input.addEventListener('input', function() {
            const cpf = this.value.replace(/\D/g, '');
            
            // Aplica máscara
            if (cpf.length <= 11) {
                this.value = cpf.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, '$1.$2.$3-$4');
            }
            
            // Valida CPF
            if (cpf.length === 11) {
                const isValid = validateCPF(cpf);
                toggleValidationFeedback(this, isValid, 'CPF inválido');
            } else if (cpf.length > 0) {
                toggleValidationFeedback(this, false, 'CPF deve ter 11 dígitos');
            } else {
                clearValidationFeedback(this);
            }
        });
    });

    // Validação de CNPJ em tempo real
    const cnpjInputs = document.querySelectorAll('input[data-val-cnpj]');
    cnpjInputs.forEach(input => {
        input.addEventListener('input', function() {
            const cnpj = this.value.replace(/\D/g, '');
            
            // Aplica máscara
            if (cnpj.length <= 14) {
                this.value = cnpj.replace(/(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})/, '$1.$2.$3/$4-$5');
            }
            
            // Valida CNPJ
            if (cnpj.length === 14) {
                const isValid = validateCNPJ(cnpj);
                toggleValidationFeedback(this, isValid, 'CNPJ inválido');
            } else if (cnpj.length > 0) {
                toggleValidationFeedback(this, false, 'CNPJ deve ter 14 dígitos');
            } else {
                clearValidationFeedback(this);
            }
        });
    });

    // Validação de valores monetários
    const moneyInputs = document.querySelectorAll('input[data-val-money]');
    moneyInputs.forEach(input => {
        input.addEventListener('input', function() {
            let value = this.value.replace(/\D/g, '');
            
            if (value.length > 0) {
                // Converte para formato monetário
                value = (parseInt(value) / 100).toFixed(2);
                this.value = 'R$ ' + value.replace('.', ',').replace(/\B(?=(\d{3})+(?!\d))/g, '.');
                
                // Valida valor
                const numericValue = parseFloat(value);
                const min = parseFloat(this.getAttribute('data-val-money-min') || '0.01');
                const max = parseFloat(this.getAttribute('data-val-money-max') || '999999.99');
                
                const isValid = numericValue >= min && numericValue <= max;
                toggleValidationFeedback(this, isValid, `Valor deve estar entre R$ ${min.toFixed(2)} e R$ ${max.toFixed(2)}`);
            } else {
                clearValidationFeedback(this);
            }
        });
    });

    // Validação de telefone
    const phoneInputs = document.querySelectorAll('input[data-val-phone]');
    phoneInputs.forEach(input => {
        input.addEventListener('input', function() {
            let phone = this.value.replace(/\D/g, '');
            
            // Aplica máscara
            if (phone.length <= 11) {
                if (phone.length <= 10) {
                    this.value = phone.replace(/(\d{2})(\d{4})(\d{4})/, '($1) $2-$3');
                } else {
                    this.value = phone.replace(/(\d{2})(\d{5})(\d{4})/, '($1) $2-$3');
                }
            }
            
            // Valida telefone
            if (phone.length >= 10 && phone.length <= 11) {
                toggleValidationFeedback(this, true, '');
            } else if (phone.length > 0) {
                toggleValidationFeedback(this, false, 'Telefone deve ter 10 ou 11 dígitos');
            } else {
                clearValidationFeedback(this);
            }
        });
    });

    // Validação de CEP e busca de endereço
    const cepInputs = document.querySelectorAll('input[data-val-cep]');
    cepInputs.forEach(input => {
        input.addEventListener('input', function() {
            let cep = this.value.replace(/\D/g, '');
            
            // Aplica máscara
            if (cep.length <= 8) {
                this.value = cep.replace(/(\d{5})(\d{3})/, '$1-$2');
            }
            
            // Valida e busca CEP
            if (cep.length === 8) {
                toggleValidationFeedback(this, true, '');
                searchCEP(cep);
            } else if (cep.length > 0) {
                toggleValidationFeedback(this, false, 'CEP deve ter 8 dígitos');
            } else {
                clearValidationFeedback(this);
            }
        });
    });

    // Validação de datas
    const dateInputs = document.querySelectorAll('input[type="date"][data-val-date]');
    dateInputs.forEach(input => {
        input.addEventListener('change', function() {
            const inputDate = new Date(this.value);
            const today = new Date();
            
            const allowFuture = this.getAttribute('data-val-date-future') === 'true';
            const allowPast = this.getAttribute('data-val-date-past') === 'true';
            const maxPastDays = parseInt(this.getAttribute('data-val-date-max-past') || '3650');
            const maxFutureDays = parseInt(this.getAttribute('data-val-date-max-future') || '365');
            
            let isValid = true;
            let message = '';
            
            if (inputDate > today && !allowFuture) {
                isValid = false;
                message = 'Data não pode ser futura';
            } else if (inputDate < today && !allowPast) {
                isValid = false;
                message = 'Data não pode ser passada';
            } else if (inputDate < today) {
                const daysDiff = Math.floor((today - inputDate) / (1000 * 60 * 60 * 24));
                if (daysDiff > maxPastDays) {
                    isValid = false;
                    message = `Data não pode ser mais de ${maxPastDays} dias no passado`;
                }
            } else if (inputDate > today) {
                const daysDiff = Math.floor((inputDate - today) / (1000 * 60 * 60 * 24));
                if (daysDiff > maxFutureDays) {
                    isValid = false;
                    message = `Data não pode ser mais de ${maxFutureDays} dias no futuro`;
                }
            }
            
            toggleValidationFeedback(this, isValid, message);
        });
    });
});

// Função para validar CPF
function validateCPF(cpf) {
    if (cpf.length !== 11 || /^(\d)\1{10}$/.test(cpf)) {
        return false;
    }
    
    let sum = 0;
    for (let i = 0; i < 9; i++) {
        sum += parseInt(cpf.charAt(i)) * (10 - i);
    }
    let remainder = sum % 11;
    let digit1 = remainder < 2 ? 0 : 11 - remainder;
    
    if (parseInt(cpf.charAt(9)) !== digit1) {
        return false;
    }
    
    sum = 0;
    for (let i = 0; i < 10; i++) {
        sum += parseInt(cpf.charAt(i)) * (11 - i);
    }
    remainder = sum % 11;
    let digit2 = remainder < 2 ? 0 : 11 - remainder;
    
    return parseInt(cpf.charAt(10)) === digit2;
}

// Função para validar CNPJ
function validateCNPJ(cnpj) {
    if (cnpj.length !== 14 || /^(\d)\1{13}$/.test(cnpj)) {
        return false;
    }
    
    const weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    const weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    
    let sum = 0;
    for (let i = 0; i < 12; i++) {
        sum += parseInt(cnpj.charAt(i)) * weights1[i];
    }
    let remainder = sum % 11;
    let digit1 = remainder < 2 ? 0 : 11 - remainder;
    
    if (parseInt(cnpj.charAt(12)) !== digit1) {
        return false;
    }
    
    sum = 0;
    for (let i = 0; i < 13; i++) {
        sum += parseInt(cnpj.charAt(i)) * weights2[i];
    }
    remainder = sum % 11;
    let digit2 = remainder < 2 ? 0 : 11 - remainder;
    
    return parseInt(cnpj.charAt(13)) === digit2;
}

// Função para buscar CEP
async function searchCEP(cep) {
    try {
        const response = await fetch(`https://viacep.com.br/ws/${cep}/json/`);
        const data = await response.json();
        
        if (!data.erro) {
            // Preenche campos de endereço se existirem
            const logradouroField = document.querySelector('input[name*="Logradouro"], input[name*="Endereco"]');
            const bairroField = document.querySelector('input[name*="Bairro"]');
            const cidadeField = document.querySelector('input[name*="Cidade"]');
            const ufField = document.querySelector('input[name*="UF"], select[name*="UF"]');
            
            if (logradouroField && data.logradouro) {
                logradouroField.value = data.logradouro;
            }
            if (bairroField && data.bairro) {
                bairroField.value = data.bairro;
            }
            if (cidadeField && data.localidade) {
                cidadeField.value = data.localidade;
            }
            if (ufField && data.uf) {
                ufField.value = data.uf;
            }
        }
    } catch (error) {
        console.error('Erro ao buscar CEP:', error);
    }
}

// Função para mostrar/esconder feedback de validação
function toggleValidationFeedback(input, isValid, message) {
    const feedbackElement = input.parentElement.querySelector('.invalid-feedback') || 
                           input.parentElement.querySelector('.valid-feedback');
    
    if (isValid) {
        input.classList.remove('is-invalid');
        input.classList.add('is-valid');
        if (feedbackElement) {
            feedbackElement.style.display = 'none';
        }
    } else {
        input.classList.remove('is-valid');
        input.classList.add('is-invalid');
        
        if (feedbackElement) {
            feedbackElement.textContent = message;
            feedbackElement.className = 'invalid-feedback';
            feedbackElement.style.display = 'block';
        } else {
            const newFeedback = document.createElement('div');
            newFeedback.className = 'invalid-feedback';
            newFeedback.textContent = message;
            newFeedback.style.display = 'block';
            input.parentElement.appendChild(newFeedback);
        }
    }
}

// Função para limpar feedback de validação
function clearValidationFeedback(input) {
    input.classList.remove('is-valid', 'is-invalid');
    const feedbackElement = input.parentElement.querySelector('.invalid-feedback') || 
                           input.parentElement.querySelector('.valid-feedback');
    if (feedbackElement) {
        feedbackElement.style.display = 'none';
    }
}
