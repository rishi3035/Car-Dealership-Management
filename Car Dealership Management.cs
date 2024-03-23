using System;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using System.IO;

namespace CarDealershipManagement
{
    class Program
    {
        static int transactionIdCounter = 0;
        static List<int> availableCars = Enumerable.Range(1, 50).ToList();
        static List<CarModel> carModels = new List<CarModel>
        {
            new CarModel { Id = 1, ModelName = "Sedan" },
            new CarModel { Id = 2, ModelName = "SUV" },
            new CarModel { Id = 3, ModelName = "Truck" },
            new CarModel { Id = 4, ModelName = "Electric" },
        };

        static void Main(string[] args)
        {
            ITransactionConfirmationService confirmationService = new DefaultTransactionConfirmationService();
            UserAuthentication userAuth = new UserAuthentication();
            userAuth.DisplayWelcomeMessage();
            Console.WriteLine("");

            List<CustomerTransaction> transactions = new List<CustomerTransaction>();
            CustomerCancel can = new CustomerCancel(confirmationService, availableCars);
            int n = 0;
            while (n < 4)
            {
                Console.WriteLine("\n1. Buy Car\n2. Return Car\n3. Exit\nEnter your choice: ");
                string ch = Console.ReadLine();
                switch (ch)
                {
                    case "1":
                        CarModel selectedCarModel = SelectCarModel();
                        if (selectedCarModel != null)
                        {
                            Console.WriteLine($"You have selected: {selectedCarModel.ModelName}");
                            CarTransaction carTransaction = new CarTransaction(confirmationService, availableCars, selectedCarModel);
                            transactions.AddRange(carTransaction.BuyCar());
                        }
                        break;
                    case "2":
                        can.ReturnCar(transactions);
                        break;
                    case "3":
                        n = 4;
                        break;
                    default:
                        Console.WriteLine("\nPlease enter correct choice\n");
                        break;
                }
            }

            // Write data to Excel
            WriteToExcel(transactions);
        }

        public static CarModel SelectCarModel()
        {
            Console.WriteLine("\nSelect a car model:");
            foreach (var carModel in carModels)
            {
                Console.WriteLine($"\t{carModel.Id}. {carModel.ModelName}");
            }
            int modelId;
            Console.Write("\nEnter car model ID: ");
            while (!int.TryParse(Console.ReadLine(), out modelId) || modelId < 1 || modelId > carModels.Count)
            {
                Console.Write("Invalid selection. Please enter a valid car model ID: ");
            }
            return carModels.FirstOrDefault(c => c.Id == modelId);
        }

        public static int GetNextTransactionId()
        {
            return ++transactionIdCounter;
        }

        public static void WriteToExcel(List<CustomerTransaction> transactions)
        {
            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Transactions");

                // Headers
                worksheet.Cells[1, 1].Value = "Transaction ID";
                worksheet.Cells[1, 2].Value = "Customer Name";
                worksheet.Cells[1, 3].Value = "Purchased Car ID";

                // Data
                int row = 2;
                foreach (var transaction in transactions)
                {
                    worksheet.Cells[row, 1].Value = transaction.Id;
                    worksheet.Cells[row, 2].Value = transaction.CustomerName;
                    worksheet.Cells[row, 3].Value = transaction.PurchasedCarId;
                    row++;
                }

                // Save the file
                FileInfo excelFile = new FileInfo("CarTransactions.xlsx");
                excelPackage.SaveAs(excelFile);
            }

            Console.WriteLine("Data written to Excel successfully.");
        }
    }

    public class UserAuthentication
    {
        private Dictionary<string, string> users = new Dictionary<string, string>();
        private string username;

        public UserAuthentication()
        {
            // Pre-register a default user for simplicity
            users.Add("admin", "password");
            users.Add("john", "doe");
        }

        public void DisplayWelcomeMessage()
        {
            Console.WriteLine("Welcome to the Car Dealership Management System");
            Console.WriteLine("Do you want to login, sign up, or exit? (login/signup/exit)");
            string action = Console.ReadLine().ToLower();
            if (action == "login")
            {
                Login();
            }
            else if (action == "signup")
            {
                SignUp();
            }
            else if (action == "exit")
            {
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("Invalid choice. Exiting...");
                Environment.Exit(0);
            }
        }

        private void Login()
        {
            Console.WriteLine("Enter username:");
            username = Console.ReadLine();
            Console.WriteLine("Enter password:");
            string password = Console.ReadLine();
            if (users.ContainsKey(username) && users[username] == password)
            {
                Console.WriteLine("Login successful!");
            }
            else
            {
                Console.WriteLine("Login failed. Please check your username and password.");
                Environment.Exit(0);
            }
        }

        private void SignUp()
        {
            Console.WriteLine("Choose a username:");
            string newUsername = Console.ReadLine();
            if (users.ContainsKey(newUsername))
            {
                Console.WriteLine("Username already exists. Please choose a different username.");
                SignUp();
                return;
            }
            Console.WriteLine("Choose a password:");
            string newPassword = Console.ReadLine();
            users.Add(newUsername, newPassword);
            Console.WriteLine("Registration successful! Please login.");
            Login();
        }
    }

    public interface ITransactionConfirmationService
    {
        int Confirmation();
    }

    internal interface CustomerAction
    {
        int Confirmation();
    }

    public class DefaultTransactionConfirmationService : ITransactionConfirmationService
    {
        public int Confirmation()
        {
            Console.WriteLine("Transaction successful\n");
            return 1;
        }
    }

    public class CarModel
    {
        public int Id { get; set; }
        public string ModelName { get; set; }
    }

    public class CustomerTransaction : CustomerAction
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public int PurchasedCarId { get; set; }

        public virtual int Confirmation()
        {
            return 1;
        }
    }

    public sealed class CarTransaction : CustomerTransaction
    {
        private readonly ITransactionConfirmationService _confirmationService;
        private List<int> _availableCars;
        private readonly CarModel _selectedCarModel;

        public CarTransaction(ITransactionConfirmationService confirmationService, List<int> availableCars, CarModel selectedCarModel)
        {
            _confirmationService = confirmationService;
            _availableCars = availableCars;
            _selectedCarModel = selectedCarModel;
        }

        public List<CustomerTransaction> BuyCar()
        {
            List<CustomerTransaction> transactions = new List<CustomerTransaction>();
            Console.Write("Enter the number of cars to buy: ");
            int numberOfCars;
            while (!int.TryParse(Console.ReadLine(), out numberOfCars) || numberOfCars <= 0)
            {
                Console.Write("Invalid input. Please enter a positive integer for the number of cars to buy: ");
            }

            for (int i = 0; i < numberOfCars; i++)
            {
                Console.Write($"Enter your name for car {i + 1}: ");
                string name = Console.ReadLine();
                Console.Write($"Enter car ID for car {i + 1}: ");
                int carId;
                while (!int.TryParse(Console.ReadLine(), out carId) || carId <= 0 || !_availableCars.Contains(carId))
                {
                    Console.Write($"Invalid input or car not available. Please enter a valid car ID for car {i + 1}: ");
                }

                transactions.Add(new CustomerTransaction
                {
                    Id = Program.GetNextTransactionId(),
                    CustomerName = name,
                    PurchasedCarId = carId
                });
                _availableCars.Remove(carId);
            }

            if (_confirmationService.Confirmation() == 1)
            {
                Console.WriteLine("Successfully Bought\n");
            }
            else
            {
                Console.WriteLine("Error\n");
            }
            return transactions;
        }
    }

    public interface ICustomerCancelActions
    {
        void ReturnCar(List<CustomerTransaction> transactions);
    }

    public sealed class CustomerCancel : ICustomerCancelActions, CustomerAction
    {
        private readonly ITransactionConfirmationService _confirmationService;
        private List<int> _availableCars;

        public CustomerCancel(ITransactionConfirmationService confirmationService, List<int> availableCars)
        {
            _confirmationService = confirmationService;
            _availableCars = availableCars;
        }

        public int Confirmation()
        {
            Console.WriteLine("Successfully Returned\n");
            return 1;
        }

        public void ReturnCar(List<CustomerTransaction> transactions)
        {
            Console.Write("Enter the Transaction ID to return the car: ");
            int transactionId;
            while (!int.TryParse(Console.ReadLine(), out transactionId) || transactionId <= 0)
            {
                Console.Write("Invalid input. Please enter a positive integer for the Transaction ID: ");
            }

            CustomerTransaction transactionToReturn = transactions.Find(t => t.Id == transactionId);
            if (transactionToReturn != null)
            {
                transactions.Remove(transactionToReturn);
                _availableCars.Add(transactionToReturn.PurchasedCarId);
                _availableCars.Sort();
                _confirmationService.Confirmation();
            }
            else
            {
                Console.WriteLine($"No transaction found for ID {transactionId}.");
            }
        }
    }
}
