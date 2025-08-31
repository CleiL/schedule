# -------- build --------
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build -- --configuration=production

# -------- runtime --------
FROM nginx:1.27-alpine AS runtime

# config do nginx (proxy e SPA)
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf

# copie a saída do Angular para o nginx (use curinga p/ não errar o nome)
# dist/<nome-do-projeto>/browser
COPY --from=build /app/dist/*/browser/ /usr/share/nginx/html/

EXPOSE 80
