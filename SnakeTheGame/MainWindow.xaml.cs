using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using System;

namespace SnakeTheGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int gridSize;
        int snakeRow;
        int snakeColumn;
        int snakeLength;
        List<Rectangle> snakeParts;
        DispatcherTimer timer;
        int rowChange, columnChange;
        bool doesEdgeKill, firstGame = true, gameEnded;
        Random random = new Random();
        Rectangle apple;
        char lastKeyPressed, keyPressed;
        int timeBetweenTicks;

        private void resetGame()
        {
            snakeLength = 1;
            snakeParts = new List<Rectangle>();
            rowChange = 0;
            columnChange = 0;
            lastKeyPressed = ' ';
            keyPressed = ' ';
            gameBoard.Children.Clear();
            gameBoard.RowDefinitions.Clear();
            gameBoard.ColumnDefinitions.Clear();

            switch (getDifficulty())
            {
                case "":
                    gridSize = 3;
                    doesEdgeKill = false;
                    timeBetweenTicks = 1000;
                    break;
                case "Easy":
                    gridSize = 5;
                    doesEdgeKill = false;
                    timeBetweenTicks = 500;
                    break;
                case "Medium":
                    gridSize = 7;
                    doesEdgeKill = false;
                    timeBetweenTicks = 300;
                    break;
                case "Hard":
                    gridSize = 9;
                    doesEdgeKill = true;
                    timeBetweenTicks = 100;
                    break;
            }

        }

        private String getDifficulty()
        {
            String difficulty = selectedDifficulty.SelectedItem.ToString().Substring(selectedDifficulty.SelectedItem.ToString().IndexOf(':') + 2);
            switch (difficulty)
            {
                case "Easy":
                    return "Easy";
                case "Medium":
                    return "Medium";
                case "Hard":
                    return "Hard";
            }

            return "";
        }


        public class Score
        {
            public String username { get; set; }
            public int score { get; set; }
            public Score() { }
            public Score(String username, int score)
            {
                this.username = username;
                this.score = score;
            }

            public String getUsername()
            {
                return this.username;
            }

            public int getScore()
            {
                return this.score;
            }

            public String allDataToString()
            {
                return "Gracz: " + this.getUsername() + " Wynik: " + this.getScore();
            }

            public String toJSON()
            {
                return "{" +
                    "\"username\": " + this.username + ',' +
                    "\"score\": " + this.score +
                    "}";
            }
        }



        public class Scoreboard
        {
            public string username { get; set; }
            public string score { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            readScore();
        }

        private void readScore()
        {

            List<Score> scoreboard = new List<Score>();

            scoreboard = deserializeJSON(scoreboard);

            List<Score> SortedList = scoreboard.OrderByDescending(o => o.getScore()).ToList();

            var dane = new List<Scoreboard>();
            for (int i = 0; i < scoreboard.Count; i++)
            {
                dane.Add(new Scoreboard { username = SortedList[i].getUsername(), score = SortedList[i].getScore().ToString() });
            }

            scoreboardList.ItemsSource = dane;
        }

        private void saveScore(int score, String username)
        {
            List<Score> scoreboard = new List<Score>();

            scoreboard = deserializeJSON(scoreboard);

            username = username == "" ? "Anonimowy Gracz" : username;

            Score newScore = new Score(username, score);
            scoreboard.Add(newScore);

            saveScoreToJSON(JsonSerializer.Serialize(scoreboard));
            readScore();
        }

        private static List<Score> deserializeJSON(List<Score> scoreboard)
        {
            if (File.Exists("scores/scores.json"))
            {
                String jsonString = File.ReadAllText("scores/scores.json");
                scoreboard = JsonSerializer.Deserialize<List<Score>>(jsonString) ?? new List<Score>();
            }

            return scoreboard;
        }

        private void saveScoreToJSON(string jsonParsed)
        {
            File.WriteAllText("scores/scores.json", jsonParsed);
        }

        private void CloseApp(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeApp(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void ControlPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void StartGame(object sender, RoutedEventArgs e)
        {
            resetGame();
            StartGame();
        }

        private void BackToMenu(object sender, RoutedEventArgs e)
        {
            BackToMenu();
        }
        private void BackToMenu()
        {
            scoreboard.Visibility = Visibility.Hidden;
            difficulty.Visibility = Visibility.Hidden;
            gameGrid.Visibility = Visibility.Hidden;
        }

        private void showScoreboard(object sender, RoutedEventArgs e)
        {
            scoreboard.Visibility = Visibility.Visible;

        }

        private void showDifficulty(object sender, RoutedEventArgs e)
        {
            difficulty.Visibility = Visibility.Visible;

        }
        private void showGameBoard()
        {
            gameGrid.Visibility = Visibility.Visible;
        }

        private void StartGame()
        {
            showGameBoard();
            snakeRow = gridSize / 2;
            snakeColumn = gridSize / 2;

            for (int i = 0; i < gridSize; i++)
            {
                gameBoard.RowDefinitions.Add(new RowDefinition());
                gameBoard.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    Rectangle rect = new Rectangle();
                    rect.Stroke = Brushes.Black;
                    rect.Fill = Brushes.LightGoldenrodYellow;
                    Grid.SetRow(rect, i);
                    Grid.SetColumn(rect, j);
                    gameBoard.Children.Add(rect);
                }
            }

            this.KeyDown += MainWindow_KeyDown;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(timeBetweenTicks);
            timer.Tick += Timer_Tick;
            timer.Start();

            GenerateApple();
            setScore();
        }


        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {

            switch (e.Key)
            {
                case Key.W:
                    if (lastKeyPressed == 'S') break;
                    rowChange = -1;
                    columnChange = 0;
                    keyPressed = 'W';
                    break;
                case Key.A:
                    if (lastKeyPressed == 'D') break;
                    rowChange = 0;
                    columnChange = -1;
                    keyPressed = 'A';
                    break;
                case Key.S:
                    if (lastKeyPressed == 'W') break;
                    rowChange = 1;
                    columnChange = 0;
                    keyPressed = 'S';
                    break;
                case Key.D:
                    if (lastKeyPressed == 'A') break;
                    rowChange = 0;
                    columnChange = 1;
                    keyPressed = 'D';
                    break;
            }

        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            MoveSnake(rowChange, columnChange, keyPressed);
        }

        private void MoveSnake(int rowChange, int columnChange, char key)
        {
            snakeRow += rowChange;
            snakeColumn += columnChange;
            lastKeyPressed = key;


            if (!doesEdgeKill)
            {
                if (snakeRow < 0) snakeRow = gridSize - 1;
                if (snakeRow > gridSize - 1) snakeRow = 0;
                if (snakeColumn < 0) snakeColumn = gridSize - 1;
                if (snakeColumn > gridSize - 1) snakeColumn = 0;
            }
            else if (doesEdgeKill && (snakeRow < 0 || snakeRow > gridSize - 1 || snakeColumn < 0 || snakeColumn > gridSize - 1))
            {
                GameOver();
            }
            if (snakeParts.Count > 1)
            {
                for (int i = 0; i < snakeParts.Count - 1; i++)
                {
                    if (Grid.GetRow(snakeParts[i]) == snakeRow && Grid.GetColumn(snakeParts[i]) == snakeColumn)
                    {
                        GameOver();
                    }
                }
            }
            if (Grid.GetRow(apple) == snakeRow && Grid.GetColumn(apple) == snakeColumn)
            {
                snakeLength++;
                setScore();
                gameBoard.Children.Remove(apple);
                if (snakeLength >= gridSize * gridSize)
                {
                    WinScreen();
                }
                else
                {
                    GenerateApple();
                }
            }
            Rectangle newSnakePart = new Rectangle();
            newSnakePart.Fill = Brushes.Green;
            newSnakePart.Stroke = Brushes.Green;
            if (gameEnded && !firstGame)
            {
                snakeRow = 0;
                snakeColumn = 0;
                gameEnded = false;
            }
            Grid.SetRow(newSnakePart, snakeRow);
            Grid.SetColumn(newSnakePart, snakeColumn);
            gameBoard.Children.Add(newSnakePart);
            snakeParts.Add(newSnakePart);

            if (snakeParts.Count == 1)
            {
                DrawHead(newSnakePart);
            }
            else
            {
                var previousHead = snakeParts[snakeParts.Count - 2];
                previousHead.Fill = Brushes.Green;
                previousHead.Stroke = Brushes.Green;
                DrawHead(newSnakePart);
            }

            foreach (var part in snakeParts)
            {
                part.Stroke = Brushes.Black;
            }

            if (snakeParts.Count > snakeLength)
            {
                var tail = snakeParts.First();
                gameBoard.Children.Remove(tail);
                snakeParts.Remove(tail);
            }

        }

        private void DrawHead(Rectangle head)
        {
            head.Fill = Brushes.LightGreen;

        }

        private void GenerateApple()
        {
            int appleRow, appleColumn;
            do
            {
                appleRow = random.Next(gridSize);
                appleColumn = random.Next(gridSize);
            } while (snakeParts.Any(part => (Grid.GetRow(part) == appleRow && Grid.GetColumn(part) == appleColumn)));

            apple = new Rectangle();
            apple.Fill = Brushes.Red;
            apple.Stroke = Brushes.Black;
            Grid.SetRow(apple, appleRow);
            Grid.SetColumn(apple, appleColumn);
            gameBoard.Children.Add(apple);
        }

        private void GameOver()
        {
            String username = txtNick.Text;
            timer.Stop();
            EndGame();
            MessageBox.Show("Game Over! Your score is: " + snakeLength + "!");
            saveScore(snakeLength, username);
        }

        private void WinScreen()
        {
            String username = txtNick.Text;
            timer.Stop();
            EndGame();
            MessageBox.Show("You win! Your score is: " + snakeLength + "!");
            saveScore(snakeLength, username);
        }

        private void setScore()
        {
            scoreLabel.Content = "Score: " + snakeLength;
        }

        private void EndGame()
        {
            BackToMenu();
            gameEnded = true;
            firstGame = false;
        }
    }
}