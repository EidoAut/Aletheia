(function () {
  var lastScheme = null;

  function currentScheme() {
    return (
      document.body.getAttribute("data-md-color-scheme") ||
      document.documentElement.getAttribute("data-md-color-scheme") ||
      "default"
    );
  }

  function prepareNode(node, scheme) {
    if (!node.dataset.mermaidSource) {
      node.dataset.mermaidSource = node.textContent.trim();
    }

    if (node.dataset.processed === "true" && scheme !== lastScheme) {
      node.innerHTML = node.dataset.mermaidSource;
      node.removeAttribute("data-processed");
    }
  }

  function renderMermaid() {
    if (!window.mermaid) {
      return;
    }

    var scheme = currentScheme();
    var nodes = Array.prototype.slice.call(document.querySelectorAll(".mermaid"));

    if (!nodes.length) {
      lastScheme = scheme;
      return;
    }

    window.mermaid.initialize({
      startOnLoad: false,
      securityLevel: "strict",
      theme: scheme === "slate" ? "dark" : "default"
    });

    nodes.forEach(function (node) {
      prepareNode(node, scheme);
    });

    var pending = nodes.filter(function (node) {
      return node.dataset.mermaidSource && node.dataset.processed !== "true";
    });

    lastScheme = scheme;

    if (pending.length) {
      window.mermaid.run({ nodes: pending }).catch(function (error) {
        console.error("Mermaid rendering failed", error);
      });
    }
  }

  if (window.document$ && typeof window.document$.subscribe === "function") {
    window.document$.subscribe(function () {
      window.setTimeout(renderMermaid, 0);
    });
  } else {
    document.addEventListener("DOMContentLoaded", renderMermaid);
  }

  if (document.body) {
    new MutationObserver(function (mutations) {
      if (
        mutations.some(function (mutation) {
          return mutation.attributeName === "data-md-color-scheme";
        })
      ) {
        window.setTimeout(renderMermaid, 0);
      }
    }).observe(document.body, {
      attributes: true,
      attributeFilter: ["data-md-color-scheme"]
    });
  }
})();
