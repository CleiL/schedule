export interface ConsultaCreateDto {
  medicoId: string;
  pacienteId: string;
  dataHora: string; // ISO string
}

export interface ConsultaResponseDto {
  consultaId: string;
  medicoId: string;
  pacienteId: string;
  dataHora: string;
  especialidade?: string;
}


export interface DoctorSchedulesResponseDto {
  id: string;           // healthcareId
  name: string;
  email: string;
  crm: string;
  speciality: string;
  schedulles: {
    appointmentId?: string;
    healthcareId: string;
    patientId: string;
    day: string;   // "YYYY-MM-DD"
    hour: string;  // ISO/local conforme seu back
  }[];
}
