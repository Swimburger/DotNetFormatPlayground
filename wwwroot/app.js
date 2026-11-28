globalThis.formatPlayground = {
  async copyText(value) {
    try {
      await navigator.clipboard.writeText(value);
      return true;
    } catch {
      const area = document.createElement("textarea");
      area.value = value;
      area.setAttribute("readonly", "");
      area.style.position = "fixed";
      area.style.opacity = "0";
      document.body.append(area);
      area.select();
      const copied = document.execCommand("copy");
      area.remove();
      return copied;
    }
  },

  encodeState(json) {
    const bytes = new TextEncoder().encode(json);
    let binary = "";
    for (const byte of bytes) binary += String.fromCharCode(byte);
    return btoa(binary).replaceAll("+", "-").replaceAll("/", "_").replace(/=+$/u, "");
  },

  decodeState(value) {
    if (!value || value.length > 120000) return null;
    try {
      const base64 = value.replaceAll("-", "+").replaceAll("_", "/");
      const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, "=");
      const binary = atob(padded);
      const bytes = Uint8Array.from(binary, character => character.charCodeAt(0));
      return new TextDecoder("utf-8", { fatal: true }).decode(bytes);
    } catch {
      return null;
    }
  },

  loadInitialState() {
    const hash = location.hash.startsWith("#state=") ? location.hash.slice(7) : "";
    if (hash) return this.decodeState(hash);
    if (localStorage.getItem("format-playground:remember") !== "true") return null;
    return localStorage.getItem("format-playground:state");
  },

  isRememberEnabled() {
    return localStorage.getItem("format-playground:remember") === "true";
  },

  saveRememberedState(enabled, json) {
    if (!enabled) {
      localStorage.removeItem("format-playground:remember");
      localStorage.removeItem("format-playground:state");
      return;
    }
    localStorage.setItem("format-playground:remember", "true");
    localStorage.setItem("format-playground:state", json);
  },

  async copyLink(json) {
    try {
      const encoded = this.encodeState(json);
      if (encoded.length > 100000) return false;
      const url = new URL(location.href);
      url.hash = `state=${encoded}`;
      await navigator.clipboard.writeText(url.href);
      return true;
    } catch {
      return false;
    }
  }
};
