export interface PatientResponseDto {
  id: string;
  name: string;
  email?: string;
  cpf?: string;
}

export interface PatientCreateDto {
  name: string;
  email: string;
  cpf: string;
}

export interface PatientUpdateDto extends PatientCreateDto {
  id: string;
}
