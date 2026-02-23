using EchoEffectApp.Logic;
using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

/*
------------------------------------------------------------
Temat projektu: Efekt dźwiękowy echo

Opis:
Projekt implementuje algorytm efektu dźwiękowego echo dla sygnału audio
zapisanego w pliku WAV. Aplikacja umożliwia użytkownikowi wybór pliku,
parametrów efektu (opóźnienie, liczba odbić, feedback), trybu obliczeń
(C/C++ lub ASM) oraz liczby wątków. Interfejs graficzny pozwala w prosty
sposób sterować procesem przetwarzania dźwięku.

Data wykonania: 28.01.2026  
Semestr: 5  
Rok akademicki: 3  
Autor: Wojciech Korga
------------------------------------------------------------
*/

/*
 * Klasa MainForm
 * Główne okno aplikacji Windows Forms odpowiedzialne za interfejs użytkownika
 * oraz obsługę zdarzeń związanych z wyborem parametrów i uruchomieniem
 * przetwarzania audio.
 */
public class MainForm : Form
{
    // Etykieta informacyjna wyświetlana na górze okna
    private Label infoLabel;

    // Przycisk rozpoczynający przetwarzanie audio
    private Button startButton;

    // Przycisk wczytania pliku WAV
    private Button loadButton;

    // Etykieta informująca o statusie wczytanego pliku
    private Label fileStatusLabel;

    // Suwak określający liczbę odbić echa (bounce)
    private TrackBar bounceBar;

    // Etykieta wyświetlająca aktualną wartość liczby odbić
    private Label bounceLabel;

    // Suwak określający poziom feedbacku (tłumienia echa)
    private TrackBar feedbackBar;

    // Etykieta wyświetlająca aktualną wartość feedbacku
    private Label feedbackLabel;

    // Wybór trybu C/C++
    private RadioButton cppRadio;

    // Wybór trybu ASM
    private RadioButton asmRadio;

    // Pole numeryczne do wprowadzania opóźnienia echa (ms)
    private NumericUpDown delayInput;

    // Etykieta opisu opóźnienia
    private Label delayLabel;

    // Pole numeryczne do wprowadzania liczby wątków
    private NumericUpDown watkiInput;

    // Etykieta opisu liczby wątków
    private Label watkiLabel;

    // Ścieżka do aktualnie wczytanego pliku WAV
    private string loadedFilePath = null;

    /*
     * Konstruktor MainForm
     * Inicjalizuje okno aplikacji oraz wszystkie kontrolki interfejsu użytkownika.
     *
     * Parametry wejściowe:
     * brak
     *
     * Parametry wyjściowe:
     * brak
     */
    public MainForm()
    {
        // Ustawienia okna
        this.Text = "Echo Effect - Projekt ASM";
        this.Width = 800;
        this.Height = 600;
        this.StartPosition = FormStartPosition.CenterScreen;

        // Główny panel layoutu
        var layout = new TableLayoutPanel();
        layout.Dock = DockStyle.Fill;
        layout.ColumnCount = 1;
        layout.RowCount = 14;
        layout.AutoScroll = true;
        layout.Padding = new Padding(20);
        layout.RowStyles.Clear();

        for (int i = 0; i < layout.RowCount; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / layout.RowCount));

        // Etykieta powitalna
        infoLabel = new Label();
        infoLabel.Text = "Witaj w projekcie efektu Echo!";
        infoLabel.Font = new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold);
        infoLabel.AutoSize = true;
        infoLabel.Anchor = AnchorStyles.None;
        layout.Controls.Add(infoLabel, 0, 0);

        // Przycisk wczytania pliku 
        loadButton = new Button();
        loadButton.Text = "Wczytaj plik audio (.wav)";
        loadButton.Font = new System.Drawing.Font("Segoe UI", 12);
        loadButton.AutoSize = true;
        loadButton.Anchor = AnchorStyles.None;
        loadButton.Click += LoadButton_Click;
        layout.Controls.Add(loadButton, 0, 1);

        // Etykieta statusu pliku 
        fileStatusLabel = new Label();
        fileStatusLabel.Text = "Nie wczytano żadnego pliku.";
        fileStatusLabel.Font = new System.Drawing.Font("Segoe UI", 10);
        fileStatusLabel.AutoSize = true;
        fileStatusLabel.Anchor = AnchorStyles.None;
        layout.Controls.Add(fileStatusLabel, 0, 2);

        // RadioButton C++ / ASM 
        cppRadio = new RadioButton();
        cppRadio.Text = "Tryb C/C++";
        cppRadio.Font = new System.Drawing.Font("Segoe UI", 12);
        cppRadio.AutoSize = true;
        cppRadio.Checked = true;
        cppRadio.Anchor = AnchorStyles.None;
        layout.Controls.Add(cppRadio, 0, 3);

        asmRadio = new RadioButton();
        asmRadio.Text = "Tryb ASM";
        asmRadio.Font = new System.Drawing.Font("Segoe UI", 12);
        asmRadio.AutoSize = true;
        asmRadio.Anchor = AnchorStyles.None;
        layout.Controls.Add(asmRadio, 0, 4);

        // Ilość wątków 
        watkiLabel = new Label();
        watkiLabel.Text = "Liczba wątków (1–64), 0 wykrywa automatycznie liczbe dostępnych wątków:";
        watkiLabel.Font = new System.Drawing.Font("Segoe UI", 12);
        watkiLabel.AutoSize = true;
        watkiLabel.Anchor = AnchorStyles.None;
        layout.Controls.Add(watkiLabel, 0, 5);

        watkiInput = new NumericUpDown();
        watkiInput.Minimum = 0;
        watkiInput.Maximum = 64;
        watkiInput.Value = 0;
        watkiInput.Width = 150;
        watkiInput.Anchor = AnchorStyles.None;
        layout.Controls.Add(watkiInput, 0, 6);

        // Delay 
        delayLabel = new Label();
        delayLabel.Text = "Długość delay'u w milisekundach:";
        delayLabel.Font = new System.Drawing.Font("Segoe UI", 12);
        delayLabel.AutoSize = true;
        delayLabel.Anchor = AnchorStyles.None;
        layout.Controls.Add(delayLabel, 0, 7);

        delayInput = new NumericUpDown();
        delayInput.Minimum = 0;
        delayInput.Maximum = 2000;
        delayInput.Value = 500;
        delayInput.Width = 150;
        delayInput.Anchor = AnchorStyles.None;
        layout.Controls.Add(delayInput, 0, 8);

        // Mix + Feedback 
        var labelsPanel = new TableLayoutPanel();
        labelsPanel.ColumnCount = 2;
        labelsPanel.RowCount = 1;
        labelsPanel.Dock = DockStyle.Fill;
        labelsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        labelsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        feedbackLabel = new Label();
        feedbackLabel.Text = $"Ilość feedback'u: 50%";
        feedbackLabel.Font = new System.Drawing.Font("Segoe UI", 12);
        feedbackLabel.AutoSize = true;
        feedbackLabel.Anchor = AnchorStyles.None;

        bounceLabel = new Label();
        bounceLabel.Text = $"Ilość odbić: 2";
        bounceLabel.Font = new System.Drawing.Font("Segoe UI", 12);
        bounceLabel.AutoSize = true;
        bounceLabel.Anchor = AnchorStyles.None;

        labelsPanel.Controls.Add(feedbackLabel, 0, 0);
        labelsPanel.Controls.Add(bounceLabel, 1, 0);
        layout.Controls.Add(labelsPanel, 0, 9);

        var slidersPanel = new TableLayoutPanel();
        slidersPanel.ColumnCount = 2;
        slidersPanel.RowCount = 1;
        slidersPanel.Dock = DockStyle.Fill;
        slidersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        slidersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        feedbackBar = new TrackBar();
        feedbackBar.Minimum = 0;
        feedbackBar.Maximum = 100;
        feedbackBar.Value = 50;
        feedbackBar.Anchor = AnchorStyles.None;
        feedbackBar.ValueChanged += feedbackBar_ValueChanged;

        bounceBar = new TrackBar();
        bounceBar.Minimum = 0;
        bounceBar.Maximum = 10;
        bounceBar.Value = 2;
        bounceBar.Anchor = AnchorStyles.None;
        bounceBar.ValueChanged += bounceBar_ValueChanged;

        slidersPanel.Controls.Add(feedbackBar, 0, 0);
        slidersPanel.Controls.Add(bounceBar, 1, 0);
        layout.Controls.Add(slidersPanel, 0, 10);

        startButton = new Button();
        startButton.Text = "Rozpocznij przetwarzanie";
        startButton.Font = new System.Drawing.Font("Segoe UI", 12);
        startButton.AutoSize = true;
        startButton.Anchor = AnchorStyles.None;
        startButton.Click += StartButton_Click;
        layout.Controls.Add(startButton, 0, 11);

        this.Controls.Add(layout);
    }

    /*
     * Procedura bounceBar_ValueChanged
     * Aktualizuje etykietę liczby odbić po zmianie wartości suwaka.
     *
     * Parametry wejściowe:
     * sender – obiekt wywołujący zdarzenie
     * e      – argumenty zdarzenia
     *
     * Parametry wyjściowe:
     * brak
     */
    private void bounceBar_ValueChanged(object sender, EventArgs e)
    {
        bounceLabel.Text = $"Ilość odbić: {bounceBar.Value}";
    }

    /*
     * Procedura feedbackBar_ValueChanged
     * Aktualizuje etykietę poziomu feedbacku po zmianie wartości suwaka.
     *
     * Parametry wejściowe:
     * sender – obiekt wywołujący zdarzenie
     * e      – argumenty zdarzenia
     *
     * Parametry wyjściowe:
     * brak
     */
    private void feedbackBar_ValueChanged(object sender, EventArgs e)
    {
        feedbackLabel.Text = $"Ilość feedback'u: {feedbackBar.Value}%";
    }

    /*
     * Procedura LoadButton_Click
     * Obsługuje wczytywanie pliku WAV z dysku oraz weryfikację jego poprawności.
     *
     * Parametry wejściowe:
     * sender – obiekt wywołujący zdarzenie
     * e      – argumenty zdarzenia
     *
     * Parametry wyjściowe:
     * brak
     */
    private void LoadButton_Click(object sender, EventArgs e)
    {
        using (OpenFileDialog openFileDialog = new OpenFileDialog())
        {
            openFileDialog.Title = "Wybierz plik audio WAV";
            openFileDialog.Filter = "Pliki WAV (*.wav)|*.wav|Wszystkie pliki (*.*)|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;

                if (Path.GetExtension(filePath).ToLower() != ".wav")
                {
                    MessageBox.Show("Nieprawidłowy format pliku! Wybierz plik z rozszerzeniem .wav.",
                        "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!File.Exists(filePath))
                {
                    MessageBox.Show("Nie można znaleźć wybranego pliku.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                loadedFilePath = filePath;
                fileStatusLabel.Text = $"Wczytano plik: {Path.GetFileName(filePath)}";
                MessageBox.Show("Plik WAV został poprawnie wczytany!", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    /*
     * Procedura StartButton_Click
     * Uruchamia przetwarzanie audio z wybranymi przez użytkownika parametrami.
     *
     * Parametry wejściowe:
     * sender – obiekt wywołujący zdarzenie
     * e      – argumenty zdarzenia
     *
     * Parametry wyjściowe:
     * brak
     */
    private void StartButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(loadedFilePath))
        {
            MessageBox.Show("Nie wybrano pliku audio! Wczytaj plik WAV przed rozpoczęciem przetwarzania.",
                "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string mode = asmRadio.Checked ? "ASM" : "C/C++";
        MessageBox.Show($"Rozpoczynam przetwarzanie pliku:\n{Path.GetFileName(loadedFilePath)}\nTryb: {mode}",
            "Echo Effect", MessageBoxButtons.OK, MessageBoxIcon.Information);

        long time = EchoLogic.ProcessAudio(
            asmRadio.Checked,
            bounceBar.Value,
            feedbackBar.Value,
            (int)delayInput.Value,
            (int)watkiInput.Value,
            loadedFilePath
        );

        MessageBox.Show($"Przetwarzanie zakończone, czas wykonania {time} ms.",
            "Echo Effect", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
