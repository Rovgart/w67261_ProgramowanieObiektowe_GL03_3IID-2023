using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Threading;
// Dodaj wynik usera do bazy danych
// Napraw dodawanie wyniku 
// Od nowa wpierdzielić geograficzne pytania

namespace ConsoleQuizApp
{   class Program
    {
        static List<string> kategorie= new List<string>{"Geografia","Historia","Informatyka"};

        public static void displayCategories()
        {
            using(MySqlConnection conn= new MySqlConnection(connectionString))
            {
                conn.Open();
                string query= "SELECT DISTINCT category_name FROM categories";
                MySqlCommand categoryCommand= new MySqlCommand(query,conn);
                MySqlDataReader reader= categoryCommand.ExecuteReader();
                while(reader.Read())
                {
                    string categoryName=reader.GetString(0);
                    Console.WriteLine(categoryName);
                }
            }
        }
        static bool isCategoryValid(string selectedCategory)
    {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            conn.Open();

            string query = $"SELECT COUNT(*) FROM categories WHERE category_name = '{selectedCategory}'";
            MySqlCommand command = new MySqlCommand(query, conn);

            int count = Convert.ToInt32(command.ExecuteScalar());

            return count > 0;
        }
    }
 static void RunQuiz(string selectedCategory)
    {
        int score = 0;

        List<int> questionIds = GetShuffledQuestionIds(selectedCategory);

        foreach (var questionId in questionIds)
        {
            string questionText = GetQuestionText(questionId);

            Console.WriteLine($"Pytanie: {questionText}");

            List<(char, string)> choices = GetQuestionChoices(questionId);

            foreach (var choice in choices)
            {
                Console.WriteLine($"{choice.Item1}. {choice.Item2}");
            }

            Console.Write("Twoja odpowiedź: ");
            string userAnswer = Console.ReadLine();

            if (IsAnswerCorrect(questionId, userAnswer))
            {
                Console.WriteLine("Gratulacje! Odpowiedziałeś poprawnie.");
                score++;
            }
            else
            {
                Console.WriteLine("Niestety, odpowiedź nieprawidłowa. Koniec quizu.");
                break;
            }
        }

        Console.WriteLine($"Twój wynik: {score}");
        SaveScoreToDatabase(score);
    }


         static List<int> GetShuffledQuestionIds(string selectedCategory)
    {
        List<int> questionIds = new List<int>();

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            string query = $"SELECT question_id FROM questions WHERE category_id = (SELECT category_id FROM categories WHERE category_name = '{selectedCategory}')";
            MySqlCommand command = new MySqlCommand(query, connection);
            MySqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                int questionId = reader.GetInt32(0);
                questionIds.Add(questionId);
            }
        }

        return Shuffle(questionIds);
    }

         static List<T> Shuffle<T>(List<T> list)
    {
        Random rng = new Random();
        int n = list.Count;

        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }

        return list;
    }
    static string GetQuestionText(int questionId)
    {
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            string query = $"SELECT question_text FROM questions WHERE question_id = {questionId}";
            MySqlCommand command = new MySqlCommand(query, connection);

            return command.ExecuteScalar().ToString();
        }
    }
 static List<(char, string)> GetQuestionChoices(int questionId)
    {
        List<(char, string)> choices = new List<(char, string)>();

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            string query = $"SELECT answer_text FROM answers WHERE question_id = {questionId} ORDER BY RAND()";
            MySqlCommand command = new MySqlCommand(query, connection);
            MySqlDataReader reader = command.ExecuteReader();

            char choiceLetter = 'A';

            while (reader.Read())
            {
                string choiceText = reader.GetString(0);
                choices.Add((choiceLetter, choiceText));
                choiceLetter++;
            }
        }

        return choices;
    }
 static string GetUserAnswer()
    {
        while (true)
        {
            Console.Write("Wprowadź pełną odpowiedź: ");
            string userAnswer = Console.ReadLine();

            if (!string.IsNullOrEmpty(userAnswer))
            {
                return userAnswer;
            }
            else
            {
                Console.WriteLine("Błędny wybór. Wprowadź poprawną pełną odpowiedź.");
            }
        }
    }
static bool CheckAnswer(int questionId, string userAnswer)
{
    // Pobierz indeks poprawnej odpowiedzi do pytania
    int correctAnswerIndex = GetCorrectAnswerIndex(questionId);
    
    Console.WriteLine($"Correct Answer Index: {correctAnswerIndex}");

    // Sprawdź, czy wprowadzony indeks jest poprawny
    return correctAnswerIndex >= 0 && userAnswer.ToUpper() == ((char)('A' + correctAnswerIndex)).ToString();
}


static string GetCorrectAnswerLetter(int questionId)
{
    using (MySqlConnection connection = new MySqlConnection(connectionString))
    {
        connection.Open();

        string query = $"SELECT answer_text FROM answers WHERE question_id = {questionId} AND is_correct = 1";
        MySqlCommand command = new MySqlCommand(query, connection);
        return command.ExecuteScalar()?.ToString();
    }
}
static int GetCorrectAnswerIndex(int questionId)
{
    using (MySqlConnection connection = new MySqlConnection(connectionString))
    {
        connection.Open();

        string query = $"SELECT answer_id FROM answers WHERE question_id = {questionId} AND is_correct = 1";
        MySqlCommand command = new MySqlCommand(query, connection);

        object result = command.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) - 1 : -1; // Zwróć indeks odpowiedzi (0 dla A, 1 dla B, itd.)
    }
}

static bool IsAnswerCorrect(int questionId, string selectedAnswer)
{
    using (MySqlConnection connection = new MySqlConnection(connectionString))
    {
        connection.Open();

        // Użyj parametrów, aby uniknąć problemów z SQL Injection
        string query = $"SELECT is_correct FROM answers WHERE question_id = @questionId AND answer_text = @selectedAnswer";
        MySqlCommand command = new MySqlCommand(query, connection);
        
        // Dodaj parametry
        command.Parameters.AddWithValue("@questionId", questionId);
        command.Parameters.AddWithValue("@selectedAnswer", selectedAnswer);

        object result = command.ExecuteScalar();

        // Sprawdź, czy result istnieje i czy jest równy 1 (poprawna odpowiedź)
        return result != null && Convert.ToInt32(result) == 1;
    }
}
 static void SaveScoreToDatabase(int score)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = $"INSERT INTO users (score) VALUES ({score})";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.ExecuteNonQuery();
            }
        }
        static string connectionString="Server=localhost;Database=quiz_db;Uid=root;Pwd=user1234;";
        static void Main(string[] args)
        {
            Console.WriteLine("Witaj w Quiz Game :)");
            // Wyświetl kategorie
            while(true)
            {
                displayCategories();
                string selectedCategory=Console.ReadLine();

                if(selectedCategory.ToLower()=="exit")
                {
                    break;
                }
                if(isCategoryValid(selectedCategory))
                {
                    RunQuiz(selectedCategory);

                }
                else
                {
                    Console.WriteLine("Nieprawidłowy wybór kategorii. Spróbuj ponownie");
                }
            }
        }
        
    }
}
