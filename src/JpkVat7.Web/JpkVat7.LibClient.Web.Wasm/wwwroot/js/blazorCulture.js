window.appCulture = {
  set: (culture) => {
    localStorage.setItem("app-culture", culture);
    // optional: cookie helps if you later host with ASP.NET Core culture provider
    document.cookie = `.AspNetCore.Culture=c=${culture}|uic=${culture}; path=/`;
    location.reload();
  },
  get: () => localStorage.getItem("app-culture") || "en"
};
