using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wagenheimer.PackageHub.Editor
{
    public class PackageUpdateProgressWindow : EditorWindow
    {
        private const float MinWindowWidth = 580f;
        private const float MinWindowHeight = 440f;
        private const float DefaultWindowWidth = 720f;
        private const float DefaultWindowHeight = 560f;

        private static readonly string[] SpinnerFrames = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        private int _spinnerIndex = 0;
        private double _lastSpinnerTick = 0;
        private float _pulseWidth = 25f;
        private bool _pulseDirection = true;

        private VisualElement _root;
        private VisualElement _headerContainer;
        private VisualElement _cardsContainer;
        private ScrollView _logScrollView;
        private VisualElement _footerContainer;

        private Label _progressStatusLabel;
        private Label _progressPercentLabel;
        private VisualElement _progressFill;

        [MenuItem("Tools/Wagenheimer/Package Updates Progress...", priority = 2)]
        public static PackageUpdateProgressWindow ShowWindow(bool focus = true)
        {
            var win = GetWindow<PackageUpdateProgressWindow>(false, "Package Updates", focus);
            win.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            EditorWindowPlacement.Center(win, new Vector2(MinWindowWidth, MinWindowHeight), new Vector2(DefaultWindowWidth, DefaultWindowHeight));
            win.Show();
            if (focus) win.Focus();
            win.RebuildAll();
            return win;
        }

        public static void OpenWithSession(PackageUpdateSessionData session)
        {
            var win = ShowWindow(true);
            win.RebuildAll();
        }

        private void OnEnable()
        {
            PackageUpdateSession.OnSessionUpdated += HandleSessionUpdated;
            EditorApplication.update += OnEditorUpdate;
            EditorWindowPlacement.Center(this, new Vector2(MinWindowWidth, MinWindowHeight), new Vector2(DefaultWindowWidth, DefaultWindowHeight));
            BuildUI();
        }

        private void OnDisable()
        {
            PackageUpdateSession.OnSessionUpdated -= HandleSessionUpdated;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void HandleSessionUpdated()
        {
            RebuildAll();
        }

        private void OnEditorUpdate()
        {
            var session = PackageUpdateSession.GetCurrentSession();
            var isRunning = session != null && session.IsActive && !session.IsFinished;

            if (!isRunning) return;

            var time = EditorApplication.timeSinceStartup;
            if (time - _lastSpinnerTick < 0.08) return;

            _lastSpinnerTick = time;
            _spinnerIndex = (_spinnerIndex + 1) % SpinnerFrames.Length;
            var spinner = SpinnerFrames[_spinnerIndex];

            // Pulse indeterminate animation
            if (_pulseDirection)
            {
                _pulseWidth += 3f;
                if (_pulseWidth >= 92f) _pulseDirection = false;
            }
            else
            {
                _pulseWidth -= 3f;
                if (_pulseWidth <= 15f) _pulseDirection = true;
            }

            if (_progressStatusLabel != null)
            {
                if (EditorApplication.isCompiling)
                {
                    _progressStatusLabel.text = $"{spinner} Compilando dependências e importando pacotes no Unity...";
                    _progressStatusLabel.style.color = new StyleColor(PackageHubUIStyle.ColAmber);
                }
                else
                {
                    _progressStatusLabel.text = $"{spinner} Processando UPM e resolvendo dependências...";
                    _progressStatusLabel.style.color = new StyleColor(PackageHubUIStyle.ColAccent);
                }
            }

            if (_progressFill != null)
            {
                if (session.TotalCount > 0 && session.CompletedCount > 0)
                {
                    var pct = Mathf.Max(10f, (float)session.CompletedCount / session.TotalCount * 100f);
                    _progressFill.style.width = Length.Percent(pct);
                }
                else
                {
                    _progressFill.style.width = Length.Percent(_pulseWidth);
                }
            }
        }

        private void BuildUI()
        {
            _root = rootVisualElement;
            _root.Clear();
            _root.AddToClassList("hub-root");
            _root.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColBgDark);
            _root.style.paddingTop = 14;
            _root.style.paddingBottom = 14;
            _root.style.paddingLeft = 16;
            _root.style.paddingRight = 16;
            PackageHubUIStyle.Apply(_root);

            _headerContainer = new VisualElement();
            _root.Add(_headerContainer);

            var contentSplit = new VisualElement();
            contentSplit.style.flexGrow = 1;
            contentSplit.style.flexDirection = FlexDirection.Column;
            contentSplit.style.marginTop = 10;
            contentSplit.style.marginBottom = 10;

            // Scrollable cards list
            var cardsScroll = new ScrollView(ScrollViewMode.Vertical);
            cardsScroll.style.flexGrow = 1;
            cardsScroll.style.maxHeight = Length.Percent(62);
            cardsScroll.style.marginBottom = 10;
            _cardsContainer = new VisualElement();
            cardsScroll.Add(_cardsContainer);
            contentSplit.Add(cardsScroll);

            // Log terminal container
            var logBox = BuildLogTerminal();
            contentSplit.Add(logBox);

            _root.Add(contentSplit);

            _footerContainer = new VisualElement();
            _footerContainer.style.flexShrink = 0;
            _root.Add(_footerContainer);

            RebuildAll();
        }

        private void RebuildAll()
        {
            RebuildHeader();
            RebuildCards();
            RebuildLogs();
            RebuildFooter();
        }

        private void RebuildHeader()
        {
            if (_headerContainer == null) return;
            _headerContainer.Clear();

            var session = PackageUpdateSession.GetCurrentSession();
            var total = session != null ? session.TotalCount : 0;
            var completed = session != null ? session.CompletedCount : 0;
            var failed = session != null ? session.FailedCount : 0;
            var isRunning = session != null && session.IsActive && !session.IsFinished;
            var isDone = session != null && session.IsFinished;

            var banner = new VisualElement();
            banner.AddToClassList("hub-header");
            banner.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColCardBg);
            banner.style.borderTopWidth = 1;
            banner.style.borderRightWidth = 1;
            banner.style.borderBottomWidth = 1;
            banner.style.borderLeftWidth = 4;
            banner.style.borderTopColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            banner.style.borderRightColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            banner.style.borderBottomColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            banner.style.borderLeftColor = new StyleColor(failed > 0 ? new Color(0.94f, 0.27f, 0.27f) : (isDone ? PackageHubUIStyle.ColGreen : PackageHubUIStyle.ColAccent));
            banner.style.SetRadius(8);
            banner.style.paddingTop = 12;
            banner.style.paddingBottom = 12;
            banner.style.paddingLeft = 16;
            banner.style.paddingRight = 16;

            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.justifyContent = Justify.SpaceBetween;
            topRow.style.alignItems = Align.Center;

            var titleGroup = new VisualElement();
            var title = new Label("GERENCIADOR DE ATUALIZAÇÕES");
            title.style.fontSize = 15;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new StyleColor(PackageHubUIStyle.ColTextWhite);
            title.style.letterSpacing = 0.5f;
            titleGroup.Add(title);

            var sub = new Label("Progresso em tempo real, resolução UPM e validação estrita de integridade");
            sub.style.fontSize = 11;
            sub.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
            sub.style.marginTop = 2;
            titleGroup.Add(sub);

            topRow.Add(titleGroup);

            // Metrics badges row
            var badgesRow = new VisualElement();
            badgesRow.style.flexDirection = FlexDirection.Row;
            badgesRow.style.alignItems = Align.Center;

            badgesRow.Add(CreatePillBadge($"Total: {total}", new Color(0.2f, 0.25f, 0.35f, 0.6f), PackageHubUIStyle.ColTextWhite));
            if (isRunning)
            {
                badgesRow.Add(CreatePillBadge("🔄 Em Execução", new Color(0.1f, 0.35f, 0.5f, 0.7f), PackageHubUIStyle.ColAccent));
            }
            if (completed > 0)
            {
                badgesRow.Add(CreatePillBadge($"✔ Concluídos: {completed}", new Color(0.1f, 0.45f, 0.25f, 0.7f), PackageHubUIStyle.ColGreen));
            }
            if (failed > 0)
            {
                badgesRow.Add(CreatePillBadge($"❌ Falhas: {failed}", new Color(0.5f, 0.15f, 0.15f, 0.8f), new Color(0.95f, 0.35f, 0.35f)));
            }

            topRow.Add(badgesRow);
            banner.Add(topRow);

            // Progress bar
            var progressBox = new VisualElement();
            progressBox.style.marginTop = 12;
            progressBox.style.paddingTop = 10;
            progressBox.style.borderTopWidth = 1;
            progressBox.style.borderTopColor = new StyleColor(new Color(1f, 1f, 1f, 0.08f));

            var progressLabels = new VisualElement();
            progressLabels.style.flexDirection = FlexDirection.Row;
            progressLabels.style.justifyContent = Justify.SpaceBetween;
            progressLabels.style.alignItems = Align.Center;

            string statusText;
            string pctText;
            float fillPct;
            Color barColor;

            if (isDone)
            {
                if (failed == 0)
                {
                    statusText = "✔ Todas as atualizações foram concluídas e verificadas com sucesso!";
                    pctText = "100% Concluído";
                    fillPct = 100f;
                    barColor = PackageHubUIStyle.ColGreen;
                }
                else
                {
                    statusText = $"Aviso: {completed} atualizado(s), {failed} com falha.";
                    pctText = $"{completed}/{total}";
                    fillPct = total > 0 ? (float)completed / total * 100f : 100f;
                    barColor = new Color(0.94f, 0.27f, 0.27f);
                }
            }
            else if (isRunning)
            {
                if (EditorApplication.isCompiling)
                {
                    statusText = $"{SpinnerFrames[_spinnerIndex % SpinnerFrames.Length]} Compilando dependências e importando pacotes no Unity...";
                    barColor = PackageHubUIStyle.ColAmber;
                }
                else
                {
                    statusText = $"{SpinnerFrames[_spinnerIndex % SpinnerFrames.Length]} Atualizando pacotes via Unity Package Manager...";
                    barColor = PackageHubUIStyle.ColAccent;
                }
                pctText = total > 0 ? $"{completed}/{total} concluído(s)" : "Processando...";
                fillPct = total > 0 ? Mathf.Max(12f, (float)completed / total * 100f) : _pulseWidth;
            }
            else
            {
                statusText = "Nenhuma atualização ativa no momento.";
                pctText = "Inativo";
                fillPct = 0f;
                barColor = PackageHubUIStyle.ColTextMuted;
            }

            _progressStatusLabel = new Label(statusText);
            _progressStatusLabel.style.fontSize = 11;
            _progressStatusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _progressStatusLabel.style.color = new StyleColor(barColor);
            progressLabels.Add(_progressStatusLabel);

            _progressPercentLabel = new Label(pctText);
            _progressPercentLabel.style.fontSize = 10.5f;
            _progressPercentLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _progressPercentLabel.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
            progressLabels.Add(_progressPercentLabel);

            progressBox.Add(progressLabels);

            var track = new VisualElement();
            track.style.height = 6;
            track.style.backgroundColor = new StyleColor(new Color(0.08f, 0.10f, 0.14f));
            track.style.SetRadius(3);
            track.style.marginTop = 6;
            track.style.overflow = Overflow.Hidden;

            _progressFill = new VisualElement();
            _progressFill.style.height = Length.Percent(100);
            _progressFill.style.width = Length.Percent(fillPct);
            _progressFill.style.backgroundColor = new StyleColor(barColor);
            _progressFill.style.SetRadius(3);
            track.Add(_progressFill);

            progressBox.Add(track);
            banner.Add(progressBox);

            _headerContainer.Add(banner);
        }

        private void RebuildCards()
        {
            if (_cardsContainer == null) return;
            _cardsContainer.Clear();

            var session = PackageUpdateSession.GetCurrentSession();
            if (session == null || session.Items == null || session.Items.Count == 0)
            {
                var emptyLabel = new Label("Nenhum item na fila de atualização.");
                emptyLabel.style.fontSize = 12;
                emptyLabel.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyLabel.style.paddingTop = 30;
                emptyLabel.style.paddingBottom = 30;
                _cardsContainer.Add(emptyLabel);
                return;
            }

            foreach (var item in session.Items)
            {
                _cardsContainer.Add(BuildItemCard(item));
            }
        }

        private VisualElement BuildItemCard(PackageUpdateItemState item)
        {
            var card = new VisualElement();
            card.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColCardBg);
            card.style.borderTopWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 4;
            card.style.borderTopColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            card.style.borderRightColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            card.style.borderBottomColor = new StyleColor(PackageHubUIStyle.ColCardBorder);

            Color accentColor;
            string statusBadgeText;
            Color badgeBg;
            Color badgeText;

            switch (item.Status)
            {
                case UpdateItemStatus.Success:
                    accentColor = PackageHubUIStyle.ColGreen;
                    statusBadgeText = $"✔ v{item.VerifiedVersion ?? item.TargetVersion} Verificado";
                    badgeBg = new Color(0.10f, 0.45f, 0.25f, 0.35f);
                    badgeText = PackageHubUIStyle.ColGreen;
                    break;
                case UpdateItemStatus.Failed:
                    accentColor = new Color(0.94f, 0.27f, 0.27f);
                    statusBadgeText = "❌ Falha";
                    badgeBg = new Color(0.55f, 0.15f, 0.15f, 0.35f);
                    badgeText = new Color(0.95f, 0.35f, 0.35f);
                    break;
                case UpdateItemStatus.Updating:
                    accentColor = PackageHubUIStyle.ColAccent;
                    statusBadgeText = "🔄 Atualizando UPM...";
                    badgeBg = new Color(0.12f, 0.35f, 0.55f, 0.35f);
                    badgeText = PackageHubUIStyle.ColAccent;
                    break;
                case UpdateItemStatus.Verifying:
                    accentColor = PackageHubUIStyle.ColAmber;
                    statusBadgeText = "🔍 Verificando Disco...";
                    badgeBg = new Color(0.55f, 0.40f, 0.10f, 0.35f);
                    badgeText = PackageHubUIStyle.ColAmber;
                    break;
                default:
                    accentColor = new Color(0.35f, 0.40f, 0.50f);
                    statusBadgeText = "⏳ Aguardando";
                    badgeBg = new Color(0.20f, 0.25f, 0.35f, 0.35f);
                    badgeText = PackageHubUIStyle.ColTextMuted;
                    break;
            }

            card.style.borderLeftColor = new StyleColor(accentColor);
            card.style.SetRadius(6);
            card.style.paddingTop = 10;
            card.style.paddingBottom = 10;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;
            card.style.marginBottom = 8;

            // Row 1: Title, PackageId and Status Badge
            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.justifyContent = Justify.SpaceBetween;
            topRow.style.alignItems = Align.Center;

            var titleBox = new VisualElement();
            titleBox.style.flexDirection = FlexDirection.Row;
            titleBox.style.alignItems = Align.Center;

            var nameLabel = new Label(item.DisplayName);
            nameLabel.style.fontSize = 13;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.color = new StyleColor(PackageHubUIStyle.ColTextWhite);
            titleBox.Add(nameLabel);

            var idLabel = new Label($" ({item.PackageId})");
            idLabel.style.fontSize = 10.5f;
            idLabel.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
            titleBox.Add(idLabel);

            topRow.Add(titleBox);

            var badge = CreatePillBadge(statusBadgeText, badgeBg, badgeText);
            topRow.Add(badge);
            card.Add(topRow);

            // Row 2: Version transition & detail message
            var bottomRow = new VisualElement();
            bottomRow.style.flexDirection = FlexDirection.Row;
            bottomRow.style.justifyContent = Justify.SpaceBetween;
            bottomRow.style.alignItems = Align.Center;
            bottomRow.style.marginTop = 6;

            var versionPill = new Label($"v{item.OldVersion} ➔ v{item.TargetVersion}");
            versionPill.style.fontSize = 11;
            versionPill.style.unityFontStyleAndWeight = FontStyle.Bold;
            versionPill.style.color = new StyleColor(PackageHubUIStyle.ColAccent);
            bottomRow.Add(versionPill);

            var detailText = !string.IsNullOrEmpty(item.ErrorMessage) ? item.ErrorMessage : item.StatusDetail;
            var detailLabel = new Label(detailText);
            detailLabel.style.fontSize = 11;
            detailLabel.style.color = new StyleColor(item.Status == UpdateItemStatus.Failed ? new Color(0.95f, 0.4f, 0.4f) : PackageHubUIStyle.ColTextMuted);
            detailLabel.style.whiteSpace = WhiteSpace.Normal;
            detailLabel.style.flexShrink = 1;
            detailLabel.style.marginLeft = 12;
            bottomRow.Add(detailLabel);

            card.Add(bottomRow);

            return card;
        }

        private VisualElement BuildLogTerminal()
        {
            var container = new VisualElement();
            container.style.flexShrink = 0;
            container.style.backgroundColor = new StyleColor(new Color(0.06f, 0.07f, 0.10f));
            container.style.borderTopWidth = 1;
            container.style.borderRightWidth = 1;
            container.style.borderBottomWidth = 1;
            container.style.borderLeftWidth = 1;
            container.style.borderTopColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            container.style.borderRightColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            container.style.borderBottomColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            container.style.borderLeftColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            container.style.SetRadius(6);
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            container.style.paddingLeft = 10;
            container.style.paddingRight = 10;
            container.style.height = 135;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 4;

            var logTitle = new Label("CONSOLE DE ATIVIDADES & UPM RESOLVE");
            logTitle.style.fontSize = 10.5f;
            logTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            logTitle.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
            header.Add(logTitle);

            var copyBtn = PackageHubUIStyle.CreateButton("📋 Copiar Logs", "hub-btn-secondary hub-btn-sm", () =>
            {
                var session = PackageUpdateSession.GetCurrentSession();
                if (session != null && session.Logs != null)
                {
                    EditorGUIUtility.systemCopyBuffer = string.Join("\n", session.Logs);
                    Debug.Log("[Package Hub] Logs copiados para a área de transferência.");
                }
            });
            copyBtn.style.height = 20;
            copyBtn.style.fontSize = 10;
            copyBtn.style.paddingTop = 1;
            copyBtn.style.paddingBottom = 1;
            copyBtn.style.paddingLeft = 8;
            copyBtn.style.paddingRight = 8;
            header.Add(copyBtn);

            container.Add(header);

            _logScrollView = new ScrollView(ScrollViewMode.Vertical);
            _logScrollView.style.flexGrow = 1;
            container.Add(_logScrollView);

            return container;
        }

        private void RebuildLogs()
        {
            if (_logScrollView == null) return;
            _logScrollView.Clear();

            var session = PackageUpdateSession.GetCurrentSession();
            if (session == null || session.Logs == null || session.Logs.Count == 0)
            {
                var empty = new Label("Nenhuma mensagem de log registrada.");
                empty.style.fontSize = 10.5f;
                empty.style.color = new StyleColor(new Color(0.4f, 0.45f, 0.55f));
                _logScrollView.Add(empty);
                return;
            }

            foreach (var log in session.Logs)
            {
                var line = new Label(log);
                line.style.fontSize = 10.5f;
                line.style.whiteSpace = WhiteSpace.Normal;
                line.style.marginBottom = 2;

                if (log.Contains("❌") || log.Contains("falhou") || log.Contains("Falha"))
                {
                    line.style.color = new StyleColor(new Color(0.95f, 0.40f, 0.40f));
                }
                else if (log.Contains("✅") || log.Contains("concluído") || log.Contains("sucesso"))
                {
                    line.style.color = new StyleColor(PackageHubUIStyle.ColGreen);
                }
                else
                {
                    line.style.color = new StyleColor(new Color(0.75f, 0.80f, 0.88f));
                }

                _logScrollView.Add(line);
            }

            _logScrollView.ScrollTo(_logScrollView.contentContainer.Children().LastOrDefault());
        }

        private void RebuildFooter()
        {
            if (_footerContainer == null) return;
            _footerContainer.Clear();

            var session = PackageUpdateSession.GetCurrentSession();
            var isRunning = session != null && session.IsActive && !session.IsFinished;
            var isDone = session != null && session.IsFinished;
            var hasFailed = session != null && session.FailedCount > 0;

            var footerRow = new VisualElement();
            footerRow.style.flexDirection = FlexDirection.Row;
            footerRow.style.justifyContent = Justify.SpaceBetween;
            footerRow.style.alignItems = Align.Center;
            footerRow.style.paddingTop = 6;

            var leftBox = new VisualElement();
            leftBox.style.flexDirection = FlexDirection.Row;
            leftBox.style.alignItems = Align.Center;

            var openHubBtn = PackageHubUIStyle.CreateButton("📦 Abrir Package Hub", "hub-btn-secondary", () =>
            {
                PackageHubWindow.ShowWindow();
            });
            openHubBtn.style.marginRight = 8;
            leftBox.Add(openHubBtn);

            if (hasFailed)
            {
                var retryBtn = PackageHubUIStyle.CreateButton("🔄 Tentar Novamente (Falhas)", "hub-btn-warning", () =>
                {
                    PackageInstaller.RetryFailed();
                });
                retryBtn.style.backgroundColor = new StyleColor(new Color(0.85f, 0.45f, 0.1f));
                retryBtn.style.marginRight = 8;
                leftBox.Add(retryBtn);
            }

            footerRow.Add(leftBox);

            var rightBox = new VisualElement();
            rightBox.style.flexDirection = FlexDirection.Row;
            rightBox.style.alignItems = Align.Center;

            if (isRunning)
            {
                var waitingLabel = new Label("Processando dependências... Por favor aguarde.");
                waitingLabel.style.fontSize = 11;
                waitingLabel.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
                waitingLabel.style.marginRight = 10;
                rightBox.Add(waitingLabel);
            }
            else
            {
                var closeBtn = PackageHubUIStyle.CreateButton("✔ Concluir / Fechar", "hub-btn-primary", () =>
                {
                    Close();
                });
                closeBtn.style.minWidth = 140;
                rightBox.Add(closeBtn);
            }

            footerRow.Add(rightBox);
            _footerContainer.Add(footerRow);
        }

        private static VisualElement CreatePillBadge(string text, Color bg, Color textColor)
        {
            var badge = new Label(text);
            badge.style.backgroundColor = new StyleColor(bg);
            badge.style.color = new StyleColor(textColor);
            badge.style.fontSize = 10.5f;
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.style.paddingTop = 3;
            badge.style.paddingBottom = 3;
            badge.style.paddingLeft = 8;
            badge.style.paddingRight = 8;
            badge.style.marginRight = 6;
            badge.style.borderTopWidth = 1;
            badge.style.borderRightWidth = 1;
            badge.style.borderBottomWidth = 1;
            badge.style.borderLeftWidth = 1;
            badge.style.borderTopColor = new StyleColor(new Color(textColor.r, textColor.g, textColor.b, 0.35f));
            badge.style.borderRightColor = new StyleColor(new Color(textColor.r, textColor.g, textColor.b, 0.35f));
            badge.style.borderBottomColor = new StyleColor(new Color(textColor.r, textColor.g, textColor.b, 0.35f));
            badge.style.borderLeftColor = new StyleColor(new Color(textColor.r, textColor.g, textColor.b, 0.35f));
            badge.style.SetRadius(12);
            return badge;
        }
    }
}
