import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const cpfPattern = /^\d{11}$/; // ajuste se usar máscara
export const crmPattern = /^CRM\/([A-Z]{2})\s?\d{4,7}$/; // Ex.: CRM/SP 123456

export function patternValidator(regex: RegExp, key: string): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null =>
    !control.value ? null : (regex.test(String(control.value)) ? null : { [key]: true });
}
