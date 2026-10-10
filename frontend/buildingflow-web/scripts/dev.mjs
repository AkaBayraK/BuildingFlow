import { spawn } from "node:child_process";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";
import {openDevelopmentBrowser} from "./browser-launch.mjs";

const require = createRequire(import.meta.url);
const cwd = fileURLToPath(new URL("../", import.meta.url));
const args = process.argv.slice(2);
const portIndex = args.findIndex((arg) => arg === "--port" || arg === "-p");
const portArgument = args.find((arg) => arg.startsWith("--port="));
const port = portIndex >= 0
  ? args[portIndex + 1]
  : portArgument?.slice("--port=".length) ?? process.env.WEB_PORT ?? "3003";

if (!/^\d+$/.test(port ?? "") || Number(port) < 1 || Number(port) > 65535) {
  console.error("Invalid development port. Use a number between 1 and 65535.");
  process.exit(1);
}

const child = spawn(process.execPath, [
  require.resolve("next/dist/bin/next"),
  "dev",
  ...(portIndex < 0 && !portArgument ? ["--port", port] : []),
  ...args,
], { cwd, env: process.env, stdio: ["inherit", "pipe", "pipe"] });

let opened = false;
let recentOutput = "";
function output(chunk, stream) {
  stream.write(chunk);
  recentOutput = (recentOutput + chunk.toString().replace(/\x1b\[[0-9;]*m/g, "")).slice(-4096);
  if (opened || !/Ready in\b/.test(recentOutput)) return;
  opened = true;
  const url = `http://localhost:${port}`;
  void openDevelopmentBrowser(url).catch(() => console.warn(`Tarayıcı kontrol edilemedi. ${url} adresini elle açın.`));
}
child.stdout.on("data", (chunk) => output(chunk, process.stdout));
child.stderr.on("data", (chunk) => output(chunk, process.stderr));
for (const signal of ["SIGINT", "SIGTERM"]) {
  process.on(signal, () => child.kill(signal));
}
child.on("error", (error) => {
  console.error(`Could not start Next.js: ${error.message}`);
  process.exitCode = 1;
});
child.on("exit", (code, signal) => {
  process.exitCode = code ?? (signal === "SIGINT" ? 130 : 1);
});
