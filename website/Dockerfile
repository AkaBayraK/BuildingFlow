FROM node:24-alpine AS build
WORKDIR /repo/frontend/buildingflow-web
COPY frontend/buildingflow-web/package*.json ./
RUN npm ci
WORKDIR /repo
COPY website ./website
COPY docs/licenses/package-inventory.json ./docs/licenses/package-inventory.json
WORKDIR /repo/website
RUN node scripts/link-dependencies.mjs
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build
RUN node scripts/release.mjs --sbom /repo/website/npm.cdx.json
RUN node scripts/bundle-notices.mjs /repo/website/npm-notices
FROM node:24-alpine
WORKDIR /app/website
ENV NODE_ENV=production HOSTNAME=0.0.0.0 PORT=3000 NEXT_TELEMETRY_DISABLED=1
COPY --from=build --chown=node:node /repo/website/.next/standalone /app/
COPY --from=build --chown=node:node /repo/website/.next/static ./.next/static
COPY --from=build --chown=node:node /repo/website/public ./public
COPY --from=build /repo/website/npm.cdx.json ./LICENSES/npm.cdx.json
COPY --from=build /repo/website/npm-notices ./LICENSES/npm-notices
COPY frontend/buildingflow-web/LICENSES ./LICENSES
COPY frontend/buildingflow-web/src/components/ui/LICENSE.md ./LICENSES/shadcn-ui-LICENSE.md
COPY frontend/buildingflow-web/src/components/ui/NOTICE.md ./LICENSES/shadcn-ui-NOTICE.md
COPY website/public/images/README.md ./LICENSES/project-images.md
USER node
EXPOSE 3000
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s CMD node -e "require('http').get('http://127.0.0.1:3000/health',{headers:{Host:new URL(process.env.WEBSITE_PUBLIC_ORIGIN||'http://localhost:3004').host}},r=>{r.resume();process.exit(r.statusCode===204?0:1)}).on('error',()=>process.exit(1))"
CMD ["node","server.js"]
