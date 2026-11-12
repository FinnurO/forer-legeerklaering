/* Felles skript for alle sidene: tilbakemeldingsknapp + aktiv-markering i sidemenyen.
   Ingen avhengigheter. Knappen og dialogen injiseres her, slik at hver side kun trenger
   <script src="…/assets/js/site.js" defer></script>. */
(function () {
  'use strict';

  var REPO = 'FinnurO/forer-legeerklaering';
  var FEEDBACK_LABEL = 'tilbakemelding';

  // ---------- Sidemeny: marker aktivt avsnitt ved skrolling ----------
  var navLinks = document.querySelectorAll('.guide-nav a[data-anchor]');
  var navAnchors = {};
  navLinks.forEach(function (l) { navAnchors[l.dataset.anchor] = true; });
  var navSections = Array.prototype.filter.call(
    document.querySelectorAll('.guide-article [id]'),
    function (el) { return navAnchors[el.id]; }
  );
  if (navLinks.length && navSections.length && 'IntersectionObserver' in window) {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) {
        if (e.isIntersecting) {
          navLinks.forEach(function (l) { l.classList.toggle('active', l.dataset.anchor === e.target.id); });
        }
      });
    }, { rootMargin: '-20% 0px -70% 0px' });
    navSections.forEach(function (s) { io.observe(s); });
  }

  // ---------- Tilbakemelding: åpner GitHub sitt «nytt issue»-skjema, forhåndsutfylt med kontekst ----------
  var dialogProbe = document.createElement('dialog');
  if (typeof dialogProbe.showModal !== 'function') { return; }

  var wrap = document.createElement('div');
  wrap.innerHTML =
    '<button type="button" class="ds-button feedback-fab" data-color="accent" id="feedback-open-btn" aria-haspopup="dialog">' +
      '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path></svg>' +
      '<span class="btn-label">Gi tilbakemelding</span>' +
    '</button>' +
    '<dialog class="feedback-dialog" id="feedback-dialog" aria-labelledby="feedback-dialog-title">' +
      '<div class="feedback-dialog__body">' +
        '<h2 id="feedback-dialog-title">Gi tilbakemelding</h2>' +
        '<p class="lead">Sendes som et innspill på GitHub — du logger inn med (eller oppretter) en gratis GitHub-konto for å fullføre innsendingen der.</p>' +
        '<p class="feedback-hint">Vi tar automatisk med hvilken side og hvilket avsnitt du var på. Vil du sitere en bestemt setning: lukk dette vinduet, marker teksten på siden, og trykk «Gi tilbakemelding» på nytt — da blir den sitert her også.</p>' +
        '<div class="feedback-context" id="feedback-context"></div>' +
        '<div class="feedback-field">' +
          '<label for="feedback-text">Hva vil du si noe om?</label>' +
          '<textarea id="feedback-text" placeholder="Skriv tilbakemeldingen din her …"></textarea>' +
        '</div>' +
        '<div class="feedback-actions">' +
          '<button type="button" class="ds-button" data-variant="tertiary" data-color="neutral" id="feedback-cancel-btn">Avbryt</button>' +
          '<button type="button" class="ds-button" data-color="accent" id="feedback-submit-btn">Send inn på GitHub →</button>' +
        '</div>' +
      '</div>' +
    '</dialog>';
  while (wrap.firstChild) { document.body.appendChild(wrap.firstChild); }

  var openBtn = document.getElementById('feedback-open-btn');
  var dialog = document.getElementById('feedback-dialog');
  var cancelBtn = document.getElementById('feedback-cancel-btn');
  var submitBtn = document.getElementById('feedback-submit-btn');
  var textEl = document.getElementById('feedback-text');
  var ctxEl = document.getElementById('feedback-context');
  var pendingSelection = '';
  var pendingSection = '';

  function esc(s) { var d = document.createElement('div'); d.textContent = s; return d.innerHTML; }

  function sectionOf(node) {
    var el = node && (node.nodeType === 1 ? node : node.parentElement);
    while (el && el !== document.body) {
      if (el.id && /^(SECTION|ARTICLE|DIV|LI)$/.test(el.tagName)) {
        var h = el.querySelector('h1, h2, h3');
        return (h ? h.textContent : el.id).trim().replace(/\s+/g, ' ');
      }
      el = el.parentElement;
    }
    return '';
  }

  function gather() {
    return { url: location.href, title: document.title, section: pendingSection, selection: pendingSelection };
  }

  function renderContext(ctx) {
    var lines = ['<div><strong>Side:</strong> ' + esc(ctx.title) + '</div>'];
    if (ctx.section) { lines.push('<div><strong>Avsnitt:</strong> ' + esc(ctx.section) + '</div>'); }
    if (ctx.selection) {
      var sel = ctx.selection.length > 200 ? ctx.selection.slice(0, 200) + '…' : ctx.selection;
      lines.push('<div><strong>Merket tekst:</strong> «' + esc(sel) + '»</div>');
    }
    ctxEl.innerHTML = lines.join('');
  }

  openBtn.addEventListener('click', function () {
    var s = window.getSelection ? window.getSelection() : null;
    pendingSelection = s ? s.toString().trim().slice(0, 1500) : '';
    pendingSection = s && s.rangeCount ? sectionOf(s.getRangeAt(0).commonAncestorContainer) : '';
    renderContext(gather());
    textEl.value = '';
    dialog.showModal();
    textEl.focus();
  });
  cancelBtn.addEventListener('click', function () { dialog.close(); });
  dialog.addEventListener('click', function (e) { if (e.target === dialog) { dialog.close(); } });
  submitBtn.addEventListener('click', function () {
    var ctx = gather();
    var body = '**Side:** ' + ctx.url + '\n';
    if (ctx.section) { body += '**Avsnitt:** ' + ctx.section + '\n'; }
    if (ctx.selection) { body += '**Merket tekst:**\n> ' + ctx.selection.replace(/\n/g, '\n> ') + '\n'; }
    body += '\n---\n\n' + (textEl.value.trim() || '_(ingen tekst skrevet inn)_');
    var url = 'https://github.com/' + REPO + '/issues/new' +
      '?title=' + encodeURIComponent('Tilbakemelding: ' + ctx.title) +
      '&body=' + encodeURIComponent(body) +
      '&labels=' + encodeURIComponent(FEEDBACK_LABEL);
    window.open(url, '_blank', 'noopener');
    dialog.close();
  });
})();
