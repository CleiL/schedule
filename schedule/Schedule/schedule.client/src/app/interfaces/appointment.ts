export interface AppointmentItemDto {
  appointmentId: string;
  patientId: string;
  healthcareId: string;
  healthcareName: string;
  speciality: string;
  day: string;  // "YYYY-MM-DD"
  hour: string; // ISO
}

export interface PatientSchedulesResponseDto {
  patientId: string;
  name: string;
  email: string;
  cpf: string;
  schedulles: AppointmentItemDto[];
}

export interface CreateAppointmentDto {
  healthcareId: string;
  patientId: string;
  day: string;   // YYYY-MM-DD
  hour: string;  // ISO (UTC)
}
