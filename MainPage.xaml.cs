using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Ristinolla01
{
    public partial class MainPage : ContentPage
    {
        public enum Player
        {
            X, O
        }


        List<Button> buttons = new List<Button>();
        Random rand = new Random();
        int playerWins = 0;
        int computerWins = 0;
        private bool gameEnded = false;
        public static string _playerName = "Pelaaja"; // Pelaajan nimi
        public static bool aiIsPlaying = false; // Pelaako tekoäly
        public static bool isTwoPlayerMode = false; // Kahden pelaajan tila
        public static bool isPlayerVsAI = true; // Pelaaja vs tekoäly tila
        private DateTime pelinAlkuAika; // Pelin alkamisajan tallennus
        private bool playerHasMadeMove = false; // Onko pelaaja tehnyt siirtonsa      
        public string playerName
        {
            get { return _playerName; }
            set
            {
                _playerName = value;
                Debug.WriteLine("playerName asetettu arvoon: " + _playerName); // Testataan tallentaako muuttuja pelaajan nimen

            }
        }



        public MainPage()
        {
            InitializeComponent();

            {

            };
            // Hae pelissä käytettävät napit ja leimat
            Button ButtonExit = this.FindByName<Button>("ButtonExit");
            Button ButtonNewGame = this.FindByName<Button>("ButtonNewGame");
            Button ButtonReset = this.FindByName<Button>("ButtonReset");
            Label LabelX = this.FindByName<Label>("LabelX");
            Label LabelXScore = this.FindByName<Label>("LabelXScore");
            Label LabelOScore = this.FindByName<Label>("LabelOScore");
            Button[] gameButtons = {
                this.FindByName<Button>("Button1"),
                this.FindByName<Button>("Button2"),
                this.FindByName<Button>("Button3"),
                this.FindByName<Button>("Button4"),
                this.FindByName<Button>("Button5"),
                this.FindByName<Button>("Button6"),
                this.FindByName<Button>("Button7"),
                this.FindByName<Button>("Button8"),
                this.FindByName<Button>("Button9"),
            };


            buttons.AddRange(gameButtons);

            // Aseta tapahtumankäsittelijä jokaiselle napille
            foreach (var button in buttons)
            {
                button.Clicked += PlayerClick;
            }

            // Alusta peli
            ResetGame(null, EventArgs.Empty);
        }

        // Uusi konstruktori pelaajanNimi-parametrilla
        public MainPage(string pelaajanNimi, Pelaaja pelaaja) : this()
        {
            playerName = pelaaja.Etunimi;
        }

        // Aseta pelitila (kahden pelaajan tila)
        public static void SetGameMode(bool twoPlayerMode)
        {
            isTwoPlayerMode = twoPlayerMode;

        }

        // Aseta pelitila (pelaaja vs tekoäly)
        public static void SetIsPlayerVsAI(bool value)
        {
            isPlayerVsAI = value;
        }

        // Aseta pelaajan nimi
        public static void AsetaPelaajanNimi(String nimi)
        {
            _playerName = nimi; // Tämä kutsuu nyt setteriä ja tulostaa arvon konsoliin
        }


        // Päivittää ilmoituksen siitä kenen vuoro on menossa
        private void UpdateTurnLabel()
        {
            if (gameEnded) return;

            if (isTwoPlayerMode)
            {
                TurnLabel.Text = playerHasMadeMove ? "Pelaaja O:n vuoro" : "Pelaaja X:n vuoro";
            }
            else
            {
                TurnLabel.Text = playerHasMadeMove ? "Tietokoneen vuoro" : "Pelaajan vuoro";
            }
        }


        // Tapahtuma, kun pelaaja klikkaa nappia
        async private void PlayerClick(Object sender, EventArgs e)
        {
            pelinAlkuAika = DateTime.Now;
            Debug.WriteLine($"aiIsPlaying: {aiIsPlaying}, gameEnded: {gameEnded}");
            // Tarkistetaan ensin onko peli meneillään ja ettei peli ole jo päättynyt
            if (aiIsPlaying || (isTwoPlayerMode && gameEnded)) return;

            var button = sender as Button;
            if (button != null && button.IsEnabled)
            {
                // Asetetaan nappulan teksti pelaajan siirron perusteella
                button.Text = playerHasMadeMove ? "O" : "X";
                button.IsEnabled = false;

                // Päivitä vuoro
                playerHasMadeMove = !playerHasMadeMove;
                UpdateTurnLabel();

                await Check();

                // Jos kyseessä on tekoälypeli ja peli ei ole päättynyt, anna tekoälyn tehdä siirto
                if (!isTwoPlayerMode && !gameEnded)
                {
                    AIMove();
                }
            }
        }

        // Tekoälyn siirto
        private async void AIMove()
        {
            aiIsPlaying = true;

            try
            {
                if (gameEnded) return;

                // AI "miettimisaika"
                int delayMilliseconds = rand.Next(500, 2001);
                await Task.Delay(delayMilliseconds);

                List<Button> availableButtons = buttons.Where(b => b.IsEnabled).ToList();

                if (availableButtons.Count > 0)
                {
                    int index = rand.Next(availableButtons.Count);
                    Button selectedButton = availableButtons[index];
                    selectedButton.Text = "O";
                    selectedButton.IsEnabled = false;

                    await Check();

                    if (!gameEnded)
                    {
                        playerHasMadeMove = false;
                        UpdateTurnLabel();
                    }
                }
            }
            finally
            {
                aiIsPlaying = false;
            }
        }
        
      
        // Päivittää tilastot, ilmoittaa voittajan ja kutsuu pelin päättäviä metodeja
        private async Task<bool> Check()
        {
            if (gameEnded)
                return false;

            string winnerMessage;

            // Tarkista rivit
            for (int i = 0; i <= 6; i += 3)
            {
                if (IsRowWinner(i, "X"))
                {
                    playerWins++;
                    await UpdatePlayerStats(Player.X, _playerName);

                    winnerMessage = isPlayerVsAI ? "Pelaaja voitti!" : "Pelaaja X voitti!";
                    await AnnounceWinner(winnerMessage);

                    playerHasMadeMove = true;
                    return true;
                }
                else if (IsRowWinner(i, "O"))
                {
                    computerWins++;
                    string opponent = isPlayerVsAI ? "Tietokone" : "Pelaaja O";
                    await UpdatePlayerStats(Player.O, opponent);

                    winnerMessage = isPlayerVsAI ? "AI voitti!" : "Pelaaja O voitti!";
                    await AnnounceWinner(winnerMessage);

                    playerHasMadeMove = false;
                    return true;
                }
            }


            // Tarkista sarakkeet
            for (int i = 0; i < 3; i++)
            {
                if (IsColumnWinner(i, "X"))
                {
                    playerWins++;
                    await UpdatePlayerStats(Player.X, playerName);

                    winnerMessage = isPlayerVsAI ? "Pelaaja voitti!" : "Pelaaja X voitti!";
                    await AnnounceWinner(winnerMessage);

                    playerHasMadeMove = true;
                    return true;
                }
                else if (IsColumnWinner(i, "O"))
                {
                    computerWins++;
                    string opponent = isPlayerVsAI ? "Tietokone" : "Pelaaja O";
                    await UpdatePlayerStats(Player.O, opponent);

                    winnerMessage = isPlayerVsAI ? "AI voitti!" : "Pelaaja O voitti!";
                    await AnnounceWinner(winnerMessage);

                    playerHasMadeMove = false;
                    return true;
                }
            }
            
            // Tarkista diagonaalit
            if (IsDiagonalWinner("X"))
            {
                playerWins++;
                await UpdatePlayerStats(Player.X, playerName);

                winnerMessage = isPlayerVsAI ? "Pelaaja voitti!" : "Pelaaja X voitti!";
                await AnnounceWinner(winnerMessage);

                playerHasMadeMove = true;
                return true;
            }
            else if (IsDiagonalWinner("O"))
            {
                computerWins++;
                string opponent = isPlayerVsAI ? "Tietokone" : "Pelaaja O";
                await UpdatePlayerStats(Player.O, opponent);

                winnerMessage = isPlayerVsAI ? "AI voitti!" : "Pelaaja O voitti!";
                await AnnounceWinner(winnerMessage);

                playerHasMadeMove = false;
                return true;
            }

            // Jos kaikki napit on painettu ja peli ei ole päättynyt, se on tasapeli
            if (buttons.All(button => !string.IsNullOrEmpty(button.Text)))
            {
                await UpdatePlayerStats(null, playerName);  // Lisää tasapeli sinulle
                string opponent = isPlayerVsAI ? "Tietokone" : "Pelaaja O";
                await UpdatePlayerStats(null, opponent); // Lisää tasapeli tietokoneelle tai Pelaaja O:lle
                await DisplayAlert("Tasapeli", "Ei voittajaa", "Seuraava peli");
                GameEnd();
                ResetGame(null, EventArgs.Empty);


                return false;
            }

            return false;
        }

        // Tarkistaa onko voittaja löytynyt ja palauttaa siitä tiedon Check-metodiin
        // Tarkista rivit
        private bool IsRowWinner(int startIndex, string playerSymbol)
        {
            return buttons[startIndex].Text == playerSymbol &&
                   buttons[startIndex + 1].Text == playerSymbol &&
                   buttons[startIndex + 2].Text == playerSymbol;
        }

        // Tarkista sarakkeet
        private bool IsColumnWinner(int startIndex, string playerSymbol)
        {
            return buttons[startIndex].Text == playerSymbol &&
                   buttons[startIndex + 3].Text == playerSymbol &&
                   buttons[startIndex + 6].Text == playerSymbol;
        }

        // Tarkista diagonaalit
        private bool IsDiagonalWinner(string playerSymbol)
        {
            return (buttons[0].Text == playerSymbol &&
                    buttons[4].Text == playerSymbol &&
                    buttons[8].Text == playerSymbol) ||
                   (buttons[2].Text == playerSymbol &&
                    buttons[4].Text == playerSymbol &&
                    buttons[6].Text == playerSymbol);
        }

   
        // Ilmoitus voittajasta
        private async Task AnnounceWinner(string message)
        {
            await DisplayAlert("Voitto!", message, "Seuraava peli");
            ResetGame(null, EventArgs.Empty);
        }

      
        // Päivitä pelaajatilastot
        private async Task UpdatePlayerStats(Player? winner, string playerName)
        {
            List<Pelaaja> pelaajat = PelaajaTiedotManager.LataaPelaajat();
            var pelaaja = pelaajat.FirstOrDefault(p => p.Etunimi == playerName);

            if (pelaaja == null)
                return;

            if (winner == Player.X)
            {
                pelaaja.Voitot++;
            }
            else if (winner == Player.O)
            {
                pelaaja.Tappiot++;
            }
            else // Tasapeli
            {
                pelaaja.Tasapelit++;
            }

            PelaajaTiedotManager.SavePlayers(pelaajat);
        }

        // Laskee pelissä kuluneen ajan
        private void GameEnd()
        {
            // Laske kulunut aika
            TimeSpan kulunutAika = DateTime.Now - pelinAlkuAika;


            var pelaajat = PelaajaTiedotManager.LataaPelaajat();
            var pelaaja = pelaajat.FirstOrDefault(p => p.Etunimi == playerName);

            if (pelaaja != null)
            {
                int pelienMaara = pelaaja.Voitot + pelaaja.Tappiot + pelaaja.Tasapelit;

                if (pelaaja.keskiarvo == TimeSpan.Zero)
                {
                    pelaaja.keskiarvo = kulunutAika;
                }
                else
                {
                    pelaaja.keskiarvo = new TimeSpan(
                        (pelaaja.keskiarvo.Ticks + kulunutAika.Ticks) / (pelienMaara + 1)
                    );
                }
            }

            // Päivitä pelaajan tilastot
            PelaajaTiedotManager.SavePlayers(pelaajat);
        }

        // Alusta peli uudelleen
        private void ResetGame(object sender, EventArgs e)
        {
            foreach (var button in buttons)
            {
                button.IsEnabled = true;
                button.Text = "";
            }

            gameEnded = false;

            UpdateTurnLabel();
        }

        // Metodi "Sulje peli" nappia varten
        private async void QuitButton_Clicked(object sender, EventArgs e)
        {                        
            if (Application.Current.MainPage is NavigationPage navigationPage)
            {
                await navigationPage.PopToRootAsync(); // Palaa juurisivulle
            }
            Application.Current.Quit(); // Sulje sovellus
        }
    }
}














