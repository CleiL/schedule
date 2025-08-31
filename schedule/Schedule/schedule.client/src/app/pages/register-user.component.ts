import { CommonModule } from "@angular/common";
import { Component, inject, OnInit } from "@angular/core";
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { Router, RouterModule } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { AuthService } from "../services/auth.service";
import { MatSnackBar, MatSnackBarModule } from "@angular/material/snack-bar";
import { cpfValidator } from "../validators/cpf.validator";

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
                <mat-card-title>Register</mat-card-title>
                <mat-card-subtitle>Crie sua conta no Agendor</mat-card-subtitle>
                <mat-card-subtitle>
                    retornar ao <a [routerLink]="['/login']">login</a>
                </mat-card-subtitle>
            </mat-card-header>
            <form [formGroup]="registerForm" (ngSubmit)="onRegister()">
                <mat-card-content>

                    <mat-form-field appearance="outline">
                        <mat-label>Nome</mat-label>
                        <input matInput type="text" placeholder="Enter your name" formControlName="name"  required>
                        <mat-error *ngIf="registerForm.get('nome')?.hasError('required')">
                            Nome é obrigatório.
                        </mat-error>
                        <mat-error *ngIf="registerForm.get('nome')?.hasError('minlength')">
                            Nome deve ter pelo menos 3 caracteres.
                        </mat-error>
                        <mat-error *ngIf="registerForm.get('nome')?.hasError('maxlength')">
                            Nome não pode ter mais de 50 caracteres.
                        </mat-error>
                    </mat-form-field>

                    <mat-form-field appearance="outline">
                        <mat-label>CPF</mat-label>
                        <input matInput type="text" placeholder="Enter your cpf" formControlName="cpf"  required>
                        <mat-error *ngIf="registerForm.get('cpf')?.hasError('required')">
                            CPF é obrigatório.
                        </mat-error>
                         <mat-error *ngIf="registerForm.get('cpf')?.hasError('cpf')">
                            CPF inválido.
                        </mat-error>
                    </mat-form-field>

                    <mat-form-field appearance="outline">
                        <mat-label>E-mail</mat-label>
                        <input matInput type="email" placeholder="Enter your email" formControlName="email" required>
                        <mat-error *ngIf="registerForm.get('email')?.hasError('required')">
                            E-mail é obrigatório.
                        </mat-error>
                        <mat-error *ngIf="registerForm.get('email')?.hasError('email')">
                            E-mail inválido.
                        </mat-error>
                    </mat-form-field>
    
                    <mat-form-field appearance="outline">
                        <mat-label>Senha</mat-label>
                        <input matInput type="password" placeholder="Enter your password" formControlName="password" required>
                        <mat-error *ngIf="registerForm.get('password')?.hasError('required')">
                            Senha é obrigatória.
                        </mat-error>
                        <mat-error *ngIf="registerForm.get('password')?.hasError('minlength')">
                            Senha deve ter pelo menos 8 caracteres.
                        </mat-error>
                    </mat-form-field>
                </mat-card-content>
                <mat-card-actions>
                    <button mat-flat-button>Register</button>
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

            .snackbar-success {
                background-color: #4caf50; 
                color: #fff;
            }

            .snackbar-error {
                background-color: #f44336;
                color: #fff;
            }

        }
  `]

})
export class RegisterUserComponent implements OnInit {
  registerForm!: FormGroup;
  submitted = false;

  #fb = inject(FormBuilder);
  #router = inject(Router);
  #authService = inject(AuthService);
  #snackBar = inject(MatSnackBar);

  ngOnInit(): void {
    this.registerForm = this.#fb.group({
      name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
      cpf: ['', [Validators.required, cpfValidator()]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  onRegister() {
    this.submitted = true;
    if (this.registerForm.invalid) return;

    const raw = this.registerForm.getRawValue();
    const payload = {
      ...raw,
      cpf: (raw.cpf ?? '').toString().replace(/\D/g, '')
    };

    this.#authService.register(payload).subscribe({
      next: () => {
        this.#snackBar.open('Registro concluído com sucesso!', 'Fechar', {
          duration: 3000,
          panelClass: ['snackbar-success']
        });
        this.#router.navigate(['/login']);
      },
      error: err => {
        const errorMessage = err.error?.detail || 'Erro inesperado';
        this.#snackBar.open(`Erro no registro: ${errorMessage}`, 'Fechar', {
          duration: 5000,
          panelClass: ['snackbar-error']
        });
      }
    });
  }
}
