import { HttpInterceptorFn } from '@angular/common/http';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('access_token');
  return next(
    token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req
  );
};
