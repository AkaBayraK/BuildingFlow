import {spawn as spawnProcess} from "node:child_process";

// ES5-compatible: the same matcher runs in Node tests and macOS JavaScript for Automation.
export function isBuildingFlowTab(candidate, origin, authority, clientId) {
  if (typeof candidate !== "string") return false;
  if (candidate === origin || candidate.indexOf(origin + "/") === 0 ||
      candidate.indexOf(origin + "?") === 0 || candidate.indexOf(origin + "#") === 0) return true;
  var authPrefix = authority.replace(/\/$/, "") + "/protocol/openid-connect/auth?";
  if (candidate.indexOf(authPrefix) !== 0) return false;
  var parameters = candidate.slice(authPrefix.length).split("#")[0].split("&");
  var client = null, redirect = null;
  try {
    for (var i = 0; i < parameters.length; i++) {
      var pair = parameters[i].split("=");
      var key = decodeURIComponent(pair[0].replace(/\+/g, " "));
      if (key !== "client_id" && key !== "redirect_uri") continue;
      var value = decodeURIComponent(pair.slice(1).join("=").replace(/\+/g, " "));
      if (key === "client_id") { if (client !== null) return false; client = value; }
      else { if (redirect !== null) return false; redirect = value; }
    }
  } catch (error) { return false; }
  return client === clientId && typeof redirect === "string" &&
    (redirect === origin || redirect.indexOf(origin + "/") === 0);
}

// Only the result enum leaves this process. Tab URLs, OIDC state and credentials are never logged.
export const macBrowserScript = isBuildingFlowTab.toString() + `
function run(argv) {
  try {
    var chrome = Application("com.google.Chrome");
    if (chrome.running()) {
      var windows = chrome.windows();
      for (var w = 0; w < windows.length; w++) {
        var tabs = windows[w].tabs();
        for (var t = 0; t < tabs.length; t++) {
          if (isBuildingFlowTab(tabs[t].url(), argv[0], argv[1], argv[2])) {
            windows[w].activeTabIndex = t + 1;
            windows[w].index = 1;
            chrome.activate();
            return "reused";
          }
        }
      }
    }
    var safari = Application("com.apple.Safari");
    if (safari.running()) {
      var safariWindows = safari.windows();
      for (var sw = 0; sw < safariWindows.length; sw++) {
        var safariTabs = safariWindows[sw].tabs();
        for (var st = 0; st < safariTabs.length; st++) {
          if (isBuildingFlowTab(safariTabs[st].url(), argv[0], argv[1], argv[2])) {
            safariWindows[sw].currentTab = safariTabs[st];
            safariWindows[sw].index = 1;
            safari.activate();
            return "reused";
          }
        }
      }
    }
    return "not-found";
  } catch (error) { return "unavailable"; }
}
`;

function runCommand(spawn, command, args, input) {
  return new Promise(resolve => {
    let child;
    try { child = spawn(command, args, {stdio: [input ? "pipe" : "ignore", "pipe", "ignore"], timeout: 8000}); }
    catch { resolve(null); return; }
    let output = "";
    child.stdout.on("data", chunk => { output = (output + chunk.toString()).slice(-256); });
    child.on("error", () => resolve(null));
    child.on("close", code => resolve(code === 0 ? output.trim() : null));
    if (input) { child.stdin.on("error", () => {}); child.stdin.end(input); }
  });
}

export async function openDevelopmentBrowser(url, {
  platform = process.platform, mode = process.env.BUILDINGFLOW_OPEN_BROWSER,
  authority = process.env.NEXT_PUBLIC_OIDC_AUTHORITY ?? "http://localhost:8080/realms/buildingflow",
  clientId = process.env.NEXT_PUBLIC_OIDC_CLIENT_ID ?? "buildingflow-web",
  spawn = spawnProcess, info = console.info, warn = console.warn,
} = {}) {
  if (mode === "false") return "disabled";
  if (platform === "darwin" && mode !== "always") {
    const result = await runCommand(spawn, "osascript", ["-l", "JavaScript", "-", url, authority, clientId], macBrowserScript);
    if (result === "reused") { info("Açık BuildingFlow sekmesi kullanılıyor; mevcut sayfa korunuyor."); return "reused"; }
    if (result !== "not-found") {
      warn(`Açık sekme kontrol edilemedi; yeni sekme açılmadı. Mevcut sekmeyi yenileyin veya ${url} adresini açın. Otomatik yeni sekme için BUILDINGFLOW_OPEN_BROWSER=always kullanabilirsiniz.`);
      return "unavailable";
    }
  }
  const [command, args] = platform === "darwin" ? ["open", [url]]
    : platform === "win32" ? ["cmd.exe", ["/c", "start", "", url]] : ["xdg-open", [url]];
  const result = await runCommand(spawn, command, args);
  if (result === null) { warn(`Tarayıcı açılamadı. ${url} adresini elle açın.`); return "unavailable"; }
  return "opened";
}
