window.downloadTextFile = (fileName, content, mimeType) => {
  const blob = new Blob([content], { type: mimeType || "text/plain" });
  const url = URL.createObjectURL(blob);

  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  a.remove();

  URL.revokeObjectURL(url);
};

window.clickById = (id) => {
  const el = document.getElementById(id);
  if (el) el.click();
};
