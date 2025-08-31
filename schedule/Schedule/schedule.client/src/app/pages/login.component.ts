import { CommonModule } from "@angular/common";
import { Component, inject, OnInit } from "@angular/core";
import { Form, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { Router, RouterModule } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { MatSnackBar, MatSnackBarModule } from "@angular/material/snack-bar";
import { AuthService } from "../services/auth.service";
import { MatDialog } from "@angular/material/dialog";


@Component({
  selector: "app-login",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,

    // Angular Material components can be imported here if needed
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSnackBarModule

  ],
  providers: [],
  template: `
        <mat-card>
            <mat-card-header>
                <mat-card-title>Login</mat-card-title>
            </mat-card-header>
            <form [formGroup]="loginForm" (ngSubmit)="onLogin()">
                <mat-card-content>
                    <mat-form-field appearance="outline">
                        <mat-label>E-mail</mat-label>
                        <input matInput type="email" placeholder="Enter your email" formControlName="email" required>
                        <mat-error *ngIf="loginForm.get('email')?.hasError('required')">
                            E-mail é obrigatório.
                        </mat-error>
                        <mat-error *ngIf="loginForm.get('email')?.hasError('email')">
                            E-mail inválido.
                        </mat-error>
                    </mat-form-field>
    
                    <mat-form-field appearance="outline">
                        <mat-label>Senha</mat-label>
                        <input matInput type="password" placeholder="Enter your password" formControlName="password" required>
                        <mat-error *ngIf="loginForm.get('password')?.hasError('required')">
                            Senha é obrigatória.
                        </mat-error>
                        <mat-error *ngIf="loginForm.get('password')?.hasError('minlength')">
                            Senha deve ter pelo menos 8 caracteres.
                        </mat-error>
                    </mat-form-field>
                </mat-card-content>
                <mat-card-actions>
                    <button mat-flat-button>Login</button>
                    <button mat-button type="button" [routerLink]="['/register']">Registro Paciente</button>
                    <button mat-button type="button" [routerLink]="['/register-medico']">Registro Médico</button>
                </mat-card-actions>
            </form>
        </mat-card>
  `,
  styles: [`
    :host {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        width: 100%;
        height: 100%;

    }

    mat-card {
        max-width: 400px;
        margin: 50px auto;
        padding: 20px;

        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 5rem;

         mat-card-content {
                display: flex;
                flex-direction: column;
                gap: 20px;
            }

            mat-form-field {
                width: 300px;
            }

            mat-card-actions {
                display: flex;
                justify-content: center;
                width: 100%;
                gap: 10px;
            }
        
    }
  `]

})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  submitted = false;

  #fb = inject(FormBuilder);
  #router = inject(Router);
  #authService = inject(AuthService);
  #snackBar = inject(MatSnackBar);

  ngOnInit(): void {
    this.loginForm = this.#fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  onLogin() {
    this.submitted = true;

    if (this.loginForm.invalid) return;

    const loginData = this.loginForm.value;

    this.#authService.login(loginData).subscribe({
      next: () => {
        this.#snackBar.open('Login concluído com sucesso!', 'Fechar', {
          duration: 3000,
          panelClass: ['snackbar-success']
        });
        this.#router.navigate(['/home']);
      },
      error: (err) => {
        const errorMessage = err.error?.detail || 'Erro inesperado';
        this.#snackBar.open(`Erro ao realizar login: ${errorMessage}`, 'Fechar', {
          duration: 5000,
          panelClass: ['snackbar-error']
        })
      }
    });
  }

}
