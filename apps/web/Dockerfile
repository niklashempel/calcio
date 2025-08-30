FROM node:lts-alpine AS build
WORKDIR /app

ENV PNPM_HOME=/pnpm
ENV PATH="${PNPM_HOME}:$PATH"
RUN corepack enable

COPY package.json pnpm-lock.yaml ./
RUN pnpm fetch --prod false

COPY . .

RUN pnpm install --frozen-lockfile --offline \
    && pnpm build \
    && rm -rf node_modules src tests *.tsbuildinfo

FROM nginx:stable-alpine AS prod

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
HEALTHCHECK --interval=30s --timeout=3s --retries=3 CMD wget -qO- http://localhost/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
