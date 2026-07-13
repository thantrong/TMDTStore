/**
 * TVT PC ChatBot — Widget tư vấn sản phẩm
 *
 * - Lịch sử: localStorage (chung mọi trang + mọi tab)
 * - sessionStorage KHÔNG dùng cho history vì tab mới = storage trống
 * - Trả lời: stream SSE từ RAG
 * - Sản phẩm gợi ý: link cùng tab, khôi phục hội thoại sau khi chuyển trang
 */

(function () {
  "use strict";

  const widget = document.getElementById("chatbot-widget");
  if (!widget) return;

  const HISTORY_KEY = "tvt_chat_history";
  const CONV_KEY = "tvt_chat_conv";
  const OPEN_KEY = "tvt_chat_open";
  const VERSION_KEY = "tvt_chat_ui_ver";
  const UI_VERSION = "2026-07-10-v5";

  const store = window.localStorage;

  function storeGet(key) {
    try {
      return store.getItem(key);
    } catch (e) {
      return null;
    }
  }

  function storeSet(key, value) {
    try {
      store.setItem(key, value);
    } catch (e) {
      /* quota / private mode */
    }
  }

  function storeRemove(key) {
    try {
      store.removeItem(key);
    } catch (e) {
      /* ignore */
    }
  }

  // Migrate sessionStorage → localStorage (bản cũ)
  if (storeGet(VERSION_KEY) !== UI_VERSION) {
    try {
      var sources = [sessionStorage, localStorage];
      var foundHistory = storeGet(HISTORY_KEY);
      var foundConv = storeGet(CONV_KEY);

      sources.forEach(function (src) {
        if (!src) return;
        Object.keys(src).forEach(function (k) {
          if (!foundHistory && (k === HISTORY_KEY || k.indexOf("tvt_chat_history") === 0)) {
            var h = src.getItem(k);
            if (h && h !== "[]") {
              foundHistory = h;
              storeSet(HISTORY_KEY, h);
            }
          }
          if (!foundConv && (k === CONV_KEY || k.indexOf("tvt_chat_conv") === 0)) {
            var c = src.getItem(k);
            if (c) {
              foundConv = c;
              storeSet(CONV_KEY, c);
            }
          }
        });
      });

      // Dọn key cũ trên sessionStorage
      Object.keys(sessionStorage).forEach(function (k) {
        if (k.indexOf("tvt_chat_") === 0) sessionStorage.removeItem(k);
      });
    } catch (e) {
      /* ignore */
    }
    storeSet(VERSION_KEY, UI_VERSION);
  }

  const PRODUCT_ID = widget.dataset.productId || "";
  const RAG_ENDPOINT =
    widget.dataset.ragEndpoint || "http://localhost:8001/api/rag/chat/stream";

  const toggleBtn = document.getElementById("chatbot-toggle");
  const panel = document.getElementById("chatbot-panel");
  const messagesEl = document.getElementById("chatbot-messages");
  const form = document.getElementById("chatbot-form");
  const input = document.getElementById("chatbot-input");
  const loadingEl = document.getElementById("chatbot-loading");
  const minimizeBtn = document.getElementById("chatbot-minimize");
  const chatIcon = document.getElementById("chatbot-icon");
  const closeIcon = document.getElementById("chatbot-close-icon");

  let isOpen = storeGet(OPEN_KEY) === "1";
  let conversationId = storeGet(CONV_KEY) || "";
  let isSending = false;

  function getHistory() {
    try {
      const raw = storeGet(HISTORY_KEY);
      return raw ? JSON.parse(raw) : [];
    } catch {
      return [];
    }
  }

  function compactSources(sources) {
    return uniqueProducts(sources).map(function (s) {
      return {
        product_id: s.product_id || "",
        product_name: s.product_name || "",
        product_url: s.product_url || productHref(s),
        price: s.price != null ? s.price : null,
      };
    });
  }

  function addToHistory(role, content, sources) {
    const history = getHistory();
    const entry = { role: role, content: content };
    if (role === "assistant" && sources && sources.length) {
      entry.sources = compactSources(sources);
    }
    history.push(entry);
    if (history.length > 20) history.splice(0, history.length - 20);
    storeSet(HISTORY_KEY, JSON.stringify(history));
  }

  /** Cắt content khi gửi API (hiển thị local vẫn giữ đủ) — tránh 422 max_length */
  var HISTORY_CONTENT_MAX = 3500;

  function truncateForApi(text) {
    if (!text || text.length <= HISTORY_CONTENT_MAX) return text;
    return text.slice(0, HISTORY_CONTENT_MAX - 1) + "…";
  }

  /** Chỉ role+content gửi lên RAG (bỏ sources để payload gọn) */
  function historyForApi() {
    return getHistory().map(function (m) {
      return { role: m.role, content: truncateForApi(m.content) };
    });
  }

  function scrollToBottom() {
    messagesEl.scrollTop = messagesEl.scrollHeight;
  }

  function escapeHtml(text) {
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
  }

  function cleanAnswerText(text) {
    let t = text || "";
    t = t.replace(/\n*Xem chi tiết sản phẩm:[\s\S]*$/i, "");
    t = t.replace(/https?:\/\/[^\s]+\/Product\/[^\s]+/gi, "");
    t = t.replace(/(^|\n)\s*\/Product\/[^\s]+/gi, "$1");
    t = t.replace(/\n{3,}/g, "\n\n").trim();
    return t;
  }

  function formatBotHtml(text) {
    const cleaned = cleanAnswerText(text);
    let html = escapeHtml(cleaned);
    html = html.replace(/\n/g, "<br>");
    return html;
  }

  function formatPrice(price) {
    if (price == null || isNaN(price) || price <= 0) return "";
    try {
      return new Intl.NumberFormat("vi-VN").format(price) + "₫";
    } catch {
      return String(price) + "₫";
    }
  }

  function uniqueProducts(sources) {
    const seen = new Set();
    const list = [];
    for (const s of sources || []) {
      const pid = s.product_id;
      if (!pid || seen.has(pid)) continue;
      seen.add(pid);
      list.push(s);
    }
    return list.slice(0, 4);
  }

  function productHref(p) {
    if (p.product_url && p.product_url.startsWith("/")) return p.product_url;
    if (p.product_id) {
      return "/Product/Details?id=" + encodeURIComponent(p.product_id);
    }
    return "#";
  }

  function renderProductCards(hostEl, sources) {
    if (!hostEl) return;
    const old = hostEl.querySelector(".chatbot-products");
    if (old) old.remove();

    const products = uniqueProducts(sources);
    if (!products.length) return;

    const wrap = document.createElement("div");
    wrap.className = "chatbot-products mt-2.5 space-y-1.5";

    const label = document.createElement("p");
    label.className =
      "text-[10px] uppercase tracking-wider text-slate-400 font-medium mb-1 px-0.5";
    label.textContent = "Sản phẩm gợi ý";
    wrap.appendChild(label);

    products.forEach(function (p) {
      const href = productHref(p);
      const price = formatPrice(p.price);
      const name = p.product_name || p.product_id || "Sản phẩm";

      const a = document.createElement("a");
      a.href = href;
      // Cùng tab + localStorage → sang trang SP vẫn còn hội thoại
      a.className =
        "chatbot-product-card rounded-xl border border-slate-200 bg-slate-50/80 px-3 py-2.5";
      a.innerHTML =
        '<div class="flex items-start gap-2">' +
        '<div class="flex-1 min-w-0">' +
        '<p class="text-[12px] font-medium text-slate-800 leading-snug line-clamp-2">' +
        escapeHtml(name) +
        "</p>" +
        (price
          ? '<p class="text-[11px] text-brand-700 font-semibold mt-1">' +
            escapeHtml(price) +
            "</p>"
          : "") +
        "</div>" +
        '<span class="flex-shrink-0 text-[11px] font-medium text-brand-700 mt-0.5 whitespace-nowrap">Xem →</span>' +
        "</div>";
      wrap.appendChild(a);
    });

    hostEl.appendChild(wrap);
  }

  function addMessage(content, role, sources) {
    const isBot = role === "assistant";
    const div = document.createElement("div");
    div.className =
      "flex items-end gap-2.5 chatbot-message " + (isBot ? "bot" : "user");

    if (isBot) {
      div.innerHTML =
        '<div class="w-7 h-7 rounded-lg bg-brand-900 text-white flex items-center justify-center flex-shrink-0 text-[10px] font-bold">AI</div>' +
        '<div class="chatbot-bubble bg-white rounded-2xl rounded-bl-md px-3.5 py-3 shadow-sm border border-slate-100/80 max-w-[88%]">' +
        '<div class="chatbot-answer text-[13px] text-slate-700 leading-relaxed">' +
        formatBotHtml(content) +
        "</div></div>";
      messagesEl.appendChild(div);
      const bubble = div.querySelector(".chatbot-bubble");
      if (sources && sources.length) renderProductCards(bubble, sources);
    } else {
      div.innerHTML =
        '<div class="ml-auto bg-brand-800 rounded-2xl rounded-br-md px-3.5 py-2.5 shadow-sm max-w-[85%]">' +
        '<p class="text-[13px] text-white leading-relaxed">' +
        escapeHtml(content) +
        "</p></div>";
      messagesEl.appendChild(div);
    }
    scrollToBottom();
  }

  function setLoading(loading) {
    loadingEl.classList.toggle("hidden", !loading);
  }

  function createBotMessage() {
    const div = document.createElement("div");
    div.className = "flex items-end gap-2.5 chatbot-message bot";
    div.innerHTML =
      '<div class="w-7 h-7 rounded-lg bg-brand-900 text-white flex items-center justify-center flex-shrink-0 text-[10px] font-bold">AI</div>' +
      '<div class="chatbot-bubble bg-white rounded-2xl rounded-bl-md px-3.5 py-3 shadow-sm border border-slate-100/80 max-w-[88%]">' +
      '<div class="chatbot-answer text-[13px] text-slate-700 leading-relaxed"></div></div>';
    messagesEl.appendChild(div);
    scrollToBottom();
    return {
      answerEl: div.querySelector(".chatbot-answer"),
      bubbleEl: div.querySelector(".chatbot-bubble"),
    };
  }

  async function sendMessage(message) {
    if (isSending || !message.trim()) return;
    isSending = true;
    input.disabled = true;

    addMessage(message, "user");
    addToHistory("user", message);
    input.value = "";

    const history = historyForApi();
    const payload = {
      message: message,
      product_id: PRODUCT_ID || null,
      conversation_id: conversationId || null,
      conversation_history: history.slice(0, -1),
    };

    const created = createBotMessage();
    const answerEl = created.answerEl;
    const bubbleEl = created.bubbleEl;
    let fullAnswer = "";
    let sources = [];
    let hasError = false;
    setLoading(true);

    try {
      const response = await fetch(RAG_ENDPOINT, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      if (!response.ok) throw new Error("Server error: " + response.status);

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = "";

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split("\n");
        buffer = lines.pop() || "";

        for (const line of lines) {
          if (!line.startsWith("data: ")) continue;
          const raw = line.slice(6);
          if (raw === "[DONE]") continue;

          try {
            const data = JSON.parse(raw);
            const type = data.type;

            if (type === "token") {
              fullAnswer += data.content;
              answerEl.innerHTML = formatBotHtml(fullAnswer);
              scrollToBottom();
            } else if (type === "final") {
              fullAnswer = data.content || fullAnswer;
              answerEl.innerHTML = formatBotHtml(fullAnswer);
              scrollToBottom();
            } else if (type === "conversation_id") {
              conversationId = data.content;
              storeSet(CONV_KEY, conversationId);
            } else if (type === "sources") {
              sources = Array.isArray(data.content) ? data.content : [];
              renderProductCards(bubbleEl, sources);
              scrollToBottom();
            }
          } catch (e) {
            /* ignore bad SSE chunk */
          }
        }
      }
    } catch (err) {
      console.error("ChatBot error:", err);
      hasError = true;
      answerEl.textContent =
        "Xin lỗi, kết nối tư vấn đang gián đoạn. Bạn thử lại sau giúp mình nhé.";
    } finally {
      setLoading(false);
      const cleaned = cleanAnswerText(fullAnswer);
      if (!hasError && cleaned) {
        addToHistory("assistant", cleaned, sources);
        answerEl.innerHTML = formatBotHtml(cleaned);
        if (sources.length) renderProductCards(bubbleEl, sources);
      } else if (!hasError) {
        answerEl.textContent =
          "Mình chưa tìm được thông tin phù hợp. Bạn mô tả rõ hơn nhu cầu được không?";
        addToHistory("assistant", answerEl.textContent);
      }
      isSending = false;
      input.disabled = false;
      input.focus();
    }
  }

  function setOpen(open) {
    isOpen = open;
    storeSet(OPEN_KEY, open ? "1" : "0");
    if (open) {
      panel.classList.remove("opacity-0", "scale-95", "pointer-events-none");
      chatIcon.classList.add("hidden");
      closeIcon.classList.remove("hidden");
      input.focus();
      scrollToBottom();
    } else {
      panel.classList.add("opacity-0", "scale-95", "pointer-events-none");
      chatIcon.classList.remove("hidden");
      closeIcon.classList.add("hidden");
    }
  }

  toggleBtn.addEventListener("click", function () {
    setOpen(!isOpen);
  });
  minimizeBtn.addEventListener("click", function () {
    setOpen(false);
  });

  form.addEventListener("submit", function (e) {
    e.preventDefault();
    const msg = input.value.trim();
    if (msg) sendMessage(msg);
  });

  input.addEventListener("keydown", function (e) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      form.dispatchEvent(new Event("submit"));
    }
  });

  function restoreHistory() {
    const history = getHistory();
    if (history.length === 0) return;
    const greeting = messagesEl.querySelector(".chatbot-message.bot");
    if (greeting) greeting.remove();
    for (const msg of history) {
      addMessage(msg.content, msg.role, msg.sources || null);
    }
  }

  restoreHistory();
  if (isOpen) setOpen(true);
})();
