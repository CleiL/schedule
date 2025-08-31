import { AbstractControl, ValidationErrors, ValidatorFn } from "@angular/forms";

export function cpfValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value || "").replace(/\D/g, "");
    if (!value) return null;

    if (value.length !== 11) return { cpf: true };
    if (/^(\d)\1+$/.test(value)) return { cpf: true };

    let soma = 0;
    for (let i = 0; i < 9; i++) soma += parseInt(value[i]) * (10 - i);
    let resto = (soma * 10) % 11;
    if (resto === 10) resto = 0;
    if (resto !== parseInt(value[9])) return { cpf: true };

    soma = 0;
    for (let i = 0; i < 10; i++) soma += parseInt(value[i]) * (11 - i);
    resto = (soma * 10) % 11;
    if (resto === 10) resto = 0;
    if (resto !== parseInt(value[10])) return { cpf: true };

    return null; // válido!
  };
}
