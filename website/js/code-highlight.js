document.addEventListener("DOMContentLoaded", function () {
  const keywords = new Set([
    "False", "None", "True", "and", "as", "assert", "async", "await", "break", "class",
    "continue", "def", "del", "elif", "else", "except", "finally", "for", "from", "global",
    "if", "import", "in", "is", "lambda", "nonlocal", "not", "or", "pass", "raise",
    "return", "try", "while", "with", "yield"
  ]);

  function escapeHtml(text) {
    return text
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;");
  }

  function highlightPython(source) {
    const escaped = escapeHtml(source);
    const tokenPattern = /(#.*|[A-Za-z_][A-Za-z0-9_]*|[0-9]+(?:[.][0-9]+)?)/g;

    return escaped.replace(tokenPattern, function (match) {
      if (match.startsWith("#")) {
        return "<span class=" + String.fromCharCode(39) + "tok-comment" + String.fromCharCode(39) + ">" + match + "</span>";
      }

      if (/^[0-9]/.test(match)) {
        return "<span class=" + String.fromCharCode(39) + "tok-number" + String.fromCharCode(39) + ">" + match + "</span>";
      }

      if (keywords.has(match)) {
        return "<span class=" + String.fromCharCode(39) + "tok-keyword" + String.fromCharCode(39) + ">" + match + "</span>";
      }

      return match;
    });
  }

  document.querySelectorAll("pre code.language-python, pre code.language-py").forEach(function (block) {
    block.innerHTML = highlightPython(block.textContent);
  });
});
