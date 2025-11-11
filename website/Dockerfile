# syntax=docker/dockerfile:1
# Build the CRA bundle. From the repository root run:
#   docker build -t loggle-website -f website/Dockerfile website
# Then start it via:
#   docker run --rm -p 3000:3000 loggle-website

FROM node:20-alpine AS build

ENV NODE_ENV=production \
    PATH="/app/node_modules/.bin:${PATH}"

WORKDIR /app

# Install dependencies using the lockfile for reproducible builds.
COPY package*.json ./
RUN npm ci --include=dev

COPY . .
RUN npm run build

FROM node:20-alpine AS runtime

ENV NODE_ENV=production \
    PATH="/app/node_modules/.bin:${PATH}"

WORKDIR /app

RUN npm install -g serve && npm cache clean --force

COPY --from=build /app/build ./build

EXPOSE 3000

CMD ["serve", "-s", "build", "-l", "3000"]
