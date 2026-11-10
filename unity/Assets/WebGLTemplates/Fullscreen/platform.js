// Browser side of the platform bridge for Yandex Games. The game (Platform.jslib → Game.Platform.Sdk)
// only knows window.CastlesPlatform; another store (Playgama, VK...) gets its own file with the same shape.
// Outside Yandex (a phone over Wi-Fi, a local server) /sdk.js is missing and everything works as a stub:
// no cloud, and a rewarded ad gives its reward at once.
(function () {
  var P = window.CastlesPlatform = {
    ready: false,     // the SDK answered or gave up; the game waits for this before loading the save
    name: "none",     // "yandex" inside Yandex Games
    lang: "ru",
    serverTime: 0,    // ms since 1970 by Yandex's clock, 0 when unknown
    cloud: null,      // JSON {save, t} from player.getData, or null
    unity: null       // set by index.html once the game has started
  };
  var ysdk = null, player = null;
  var wantGameplay = false, gameplayOn = false, inAd = false, hidden = document.hidden;

  function send(method, arg) {
    try { if (P.unity) P.unity.SendMessage("PlatformEvents", method, arg == null ? "" : String(arg)); }
    catch (e) { console.warn("[platform] " + method + ": " + e); }
  }

  // GameplayAPI is on only while the player can actually play: not during ads, not in a hidden tab.
  function syncGameplay() {
    var on = wantGameplay && !inAd && !hidden;
    if (on === gameplayOn) return;
    gameplayOn = on;
    try {
      var g = ysdk && ysdk.features && ysdk.features.GameplayAPI;
      if (g) on ? g.start() : g.stop();
    } catch (e) { console.warn("[platform] gameplay: " + e); }
  }

  function adOpen() { inAd = true; syncGameplay(); send("OnAd", "open"); }
  function adClose(ev) { inAd = false; syncGameplay(); send("OnAd", ev || "close"); }

  function done() {
    if (P.ready) return;
    P.ready = true;
    document.documentElement.lang = P.lang;
  }

  document.addEventListener("visibilitychange", function () {
    hidden = document.hidden;
    syncGameplay();
    send("OnHidden", hidden ? "1" : "0");
  });

  // No answer in 6 seconds: start without the SDK rather than keep the player waiting.
  setTimeout(done, 6000);

  var s = document.createElement("script");
  s.src = "/sdk.js";
  s.onerror = done;
  s.onload = function () {
    if (!window.YaGames) { done(); return; }
    YaGames.init().then(function (sdk) {
      ysdk = sdk;
      P.name = "yandex";
      try { P.lang = sdk.environment.i18n.lang || "ru"; } catch (e) {}
      try { P.serverTime = sdk.serverTime() || 0; } catch (e) {}
      try {
        sdk.on("game_api_pause", function () { send("OnPause", "1"); });
        sdk.on("game_api_resume", function () { send("OnPause", "0"); });
      } catch (e) {}
      return sdk.getPlayer({ scopes: false }).then(function (p) {
        player = p;
        return p.getData(["save", "t"]);
      }).then(function (d) {
        if (d && typeof d.save === "string" && d.save) P.cloud = JSON.stringify({ save: d.save, t: d.t || 0 });
      });
    }).catch(function (e) {
      console.warn("[platform] Yandex SDK: " + e);
    }).then(done);
  };
  document.head.appendChild(s);

  P.saveCloud = function (json, t, flush) {
    if (!player) return;
    player.setData({ save: json, t: t }, !!flush).catch(function (e) { console.warn("[platform] setData: " + e); });
  };

  P.gameReady = function () {
    try {
      var l = ysdk && ysdk.features && ysdk.features.LoadingAPI;
      if (l) l.ready();
    } catch (e) { console.warn("[platform] ready: " + e); }
    wantGameplay = true;
    syncGameplay();
  };

  P.gameplay = function (on) { wantGameplay = on; syncGameplay(); };

  P.showRewarded = function () {
    if (!ysdk) {
      // Stub outside Yandex: the ad «plays» and pays at once.
      adOpen();
      setTimeout(function () { send("OnAd", "rewarded"); adClose("close"); }, 300);
      return;
    }
    ysdk.adv.showRewardedVideo({
      callbacks: {
        onOpen: adOpen,
        onRewarded: function () { send("OnAd", "rewarded"); },
        onClose: function () { adClose("close"); },
        onError: function (e) { console.warn("[platform] rewarded: " + e); adClose("error"); }
      }
    });
  };

  P.showInterstitial = function () {
    if (!ysdk) return;
    var opened = false;
    ysdk.adv.showFullscreenAdv({
      callbacks: {
        onOpen: function () { opened = true; adOpen(); },
        onClose: function () { if (opened) adClose("close"); },
        onError: function (e) { console.warn("[platform] fullscreen: " + e); if (opened) adClose("error"); }
      }
    });
  };
})();
