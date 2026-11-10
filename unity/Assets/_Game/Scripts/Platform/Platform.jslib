// Unity side of the platform bridge. Every call goes to window.CastlesPlatform (platform.js in the WebGL template),
// so a different store needs only a different platform.js, not changes here or in C#.
mergeInto(LibraryManager.library, {
  $PlatStr: function (s) {
    s = s == null ? "" : String(s);
    var n = lengthBytesUTF8(s) + 1, p = _malloc(n);
    stringToUTF8(s, p, n);
    return p;
  },
  Plat_Ready__deps: ["$PlatStr"],
  Plat_Ready: function () { var P = window.CastlesPlatform; return P && P.ready ? 1 : 0; },
  Plat_Name__deps: ["$PlatStr"],
  Plat_Name: function () { var P = window.CastlesPlatform; return PlatStr(P ? P.name : "none"); },
  Plat_Lang__deps: ["$PlatStr"],
  Plat_Lang: function () { var P = window.CastlesPlatform; return PlatStr(P ? P.lang : "ru"); },
  Plat_ServerTime: function () { var P = window.CastlesPlatform; return P ? P.serverTime || 0 : 0; },
  Plat_Cloud__deps: ["$PlatStr"],
  Plat_Cloud: function () { var P = window.CastlesPlatform; return PlatStr(P && P.cloud ? P.cloud : ""); },
  Plat_SaveCloud: function (json, t, flush) {
    var P = window.CastlesPlatform;
    if (P) P.saveCloud(UTF8ToString(json), t, flush === 1);
  },
  Plat_GameReady: function () { var P = window.CastlesPlatform; if (P) P.gameReady(); },
  Plat_Gameplay: function (on) { var P = window.CastlesPlatform; if (P) P.gameplay(on === 1); },
  Plat_ShowRewarded: function () { var P = window.CastlesPlatform; if (P) P.showRewarded(); },
  Plat_ShowInterstitial: function () { var P = window.CastlesPlatform; if (P) P.showInterstitial(); }
});
