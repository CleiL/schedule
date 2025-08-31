import { Injectable } from '@angular/core';
import { ICustomer } from '../interfaces/customer';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CustomerService {

  constructor(private http: HttpClient) { }

  private apiUrl = environment.apiUrls;

  getUser(): Observable<ICustomer> {
    return this.http.get<ICustomer>(`${this.apiUrl}/auth/customer/`);
  }

  updateUser(data: Partial<ICustomer>): Observable<ICustomer> {
    return this.http.put<ICustomer>(`${this.apiUrl}/auth/customer/`, data);
  }

  updatePassword(payload: { current_password: string, new_password: string }): Observable<any> {
    return this.http.put(`${this.apiUrl}/auth/change-password/`, payload);
  }
}
