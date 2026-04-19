FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
ARG VITE_API_BASE_URL=http://localhost:5000/api
ARG VITE_HUB_URL=http://localhost:5000/hubs/execution
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_HUB_URL=$VITE_HUB_URL
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
