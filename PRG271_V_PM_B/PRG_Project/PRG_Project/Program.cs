using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PRG_Project;

internal class Program
{
    private static SimulationEngine engine;
    private static StationEventSubscriber subscriber;
    private static StationLogger logger;
    private static void Main()
    {
        Console.WriteLine("==================================");
        Console.WriteLine(" SPACE STATION CONTROL SYSTEM");
        Console.WriteLine("==================================");

        SetupSystem();
        engine.Start();

        bool running = true;
        while (running)
        {
            PrintMenu();
            string choice = Console.ReadLine().Trim();

            try
            {
                switch (choice)
                {
                    case "1":
                        ViewAllModules();
                        break;
                    case "2":
                        AddModule();
                        break;
                    case "3":
                        RemoveModule();
                        break;
                    case "4":
                        AssignCrew();
                        break;
                    case "5":
                        RemoveCrew();
                        break;
                    case "6":
                        TransferPower();
                        break;
                    case "7":
                        RepairModule();
                        break;
                    case "8":
                        RunDiagnostics();
                        break;
                    case "9":
                        EmergencyShutdown();
                        break;
                    case "10":
                        ViewEventHistory();
                        break;
                    case "11":
                        SaveLogsToFile();
                        break;
                    case "0":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Invalid option, please try again.");
                        break;
                }
            }

            catch (ModuleOperationException ex)
            {
                Console.WriteLine($"SYSTEM ERROR! {ex.Message} (Module: {ex.ModuleName}, ID: {ex.ModuleId})");
            }
            catch (FormatException)
            {
                Console.WriteLine("INPUT ERROR! Please enter a valid number.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UNEXPECTED ERROR! {ex.Message}");
            }
            finally
            {
                Thread.Sleep(100);
            }
        }

        engine.Stop();
        logger.SaveToFile();

        Console.WriteLine("Station system shutting down. Goodbye!");
    }

    private static void SetupSystem()
    {
        engine = new SimulationEngine();
        subscriber = new StationEventSubscriber();
        engine.RegisterSubscriber(subscriber);

        logger = new StationLogger("station_log.txt");
        logger.AttachToEngine(engine);

        Random rng = new Random();

        LifeSupportModule lifeSupport = new LifeSupportModule(
            "Hab Life Support",
            powerConsumption: 10,
            capacity: rng.Next(4, 9));

        PowerGridModule powerGrid = new PowerGridModule(
            "Main Reactor",
            powerConsumption: 5,
            energyGeneration: rng.Next(30, 60),
            storageCapacity: rng.Next(200, 400));

        engine.AddModule(lifeSupport);
        engine.AddModule(powerGrid);

        Console.WriteLine("Starting modules created:");
        Console.WriteLine($"{lifeSupport.name} {lifeSupport.id}");
        Console.WriteLine($"{powerGrid.name} {powerGrid.id}");

    }

    private static void PrintMenu()
    {
        Console.WriteLine();
        Console.WriteLine("----------- MAIN MENU -----------");
        Console.WriteLine("1.  View All Modules");
        Console.WriteLine("2.  Add Module");
        Console.WriteLine("3.  Remove Module");
        Console.WriteLine("4.  Assign Crew");
        Console.WriteLine("5.  Remove Crew");
        Console.WriteLine("6.  Transfer Power");
        Console.WriteLine("7.  Repair Module");
        Console.WriteLine("8.  Run Diagnostics");
        Console.WriteLine("9.  Emergency Shutdown");
        Console.WriteLine("10. View Event Log History");
        Console.WriteLine("11. Save Logs to File");
        Console.WriteLine("0.  Exit");
        Console.Write("Select an option: ");
    }

    private static void ViewAllModules()
    {
        List<Module> modules = engine.Modules.ToList();

        if (modules.Count == 0)
        {
            Console.WriteLine("No modules registered.");
            return;
        }

        foreach (Module module in modules)
        {
            Console.WriteLine(module.GetTelemetryReport());
        }

        logger.LogModuleSnapshot(modules);
    }

    private static void AddModule()
    {
        Console.WriteLine("Select module type: 1) Life Support   2) Power Grid");
        string typeChoice = Console.ReadLine()?.Trim();

        Console.Write("Module name: ");
        string name = Console.ReadLine()?.Trim();

        double power = ReadDouble("Power consumption (kW): ");

        if (typeChoice == "1")
        {
            int capacity = ReadInt("Crew capacity: ");
            LifeSupportModule module = new LifeSupportModule(name, power, capacity);
            engine.AddModule(module);
            Console.WriteLine($"Life Support module '{name}' added ({module.id}).");
        }
        else if (typeChoice == "2")
        {
            double generation = ReadDouble("Energy generation (kW): ");
            double capacity = ReadDouble("Storage capacity (kWh): ");

            PowerGridModule module = new PowerGridModule(name, power, generation, capacity);
            engine.AddModule(module);
            Console.WriteLine($"Power Grid module '{name}' added ({module.id}).");
        }
        else
        {
            Console.WriteLine("Invalid module type selected.");
        }
    }

    private static void RemoveModule()
    {
        Console.Write("Enter module ID to remove: ");
        string id = Console.ReadLine()?.Trim();
        engine.RemoveModule(id);
    }

    private static void AssignCrew()
    {
        Console.Write("Enter Life Support module ID: ");
        string id = Console.ReadLine()?.Trim();
        int count = ReadInt("Crew to assign: ");
        engine.AssignCrew(id, count);
    }

    private static void RemoveCrew()
    {
        Console.Write("Enter Life Support module ID: ");
        string id = Console.ReadLine()?.Trim();
        int count = ReadInt("Crew to remove: ");
        engine.RemoveCrew(id, count);
    }

    private static void TransferPower()
    {
        Console.Write("Enter Power Grid module ID: ");
        string id = Console.ReadLine()?.Trim();
        double amount = ReadDouble("Amount to transfer (kWh): ");
        engine.TransferPower(id, amount);
    }

    private static void RepairModule()
    {
        Console.Write("Enter module ID to repair: ");
        string id = Console.ReadLine()?.Trim();
        int value = ReadInt("Repair value: ");
        engine.PerformRepair(id, value);
    }

    private static void RunDiagnostics()
    {
        Console.Write("Enter module ID to diagnose: ");
        string id = Console.ReadLine()?.Trim();

        Module module = engine.FindModule(id);

        if (module == null)
        {
            Console.WriteLine("Module not found.");
            return;
        }

        IMaintenance maintainable = module as IMaintenance;

        if (maintainable != null)
        {
            bool passed = maintainable.Diagnostic();

            if (passed)
            {
                Console.WriteLine($"{module.name} passed diagnostics.");
            }
            else
            {
                Console.WriteLine($"{module.name} failed diagnostics, may require maintenance.");
            }
        }
        else
        {
            Console.WriteLine($"{module.name} does not support diagnostics.");
        }
    }

    private static void EmergencyShutdown()
    {
        Console.Write("Enter module ID to shut down: ");
        string id = Console.ReadLine()?.Trim();
        engine.EmergencyShutdown(id);
    }

    private static void ViewEventHistory()
    {
        IReadOnlyList<string> history = subscriber.History;

        if (history.Count == 0)
        {
            Console.WriteLine("No events recorded yet.");
            return;
        }

        foreach (string entry in history)
        {
            Console.WriteLine(entry);
        }
    }

    private static void SaveLogsToFile()
    {
        logger.SaveToFile();
        Console.WriteLine("Logs saved to file successfully.");
    }

    
    private static int ReadInt(string prompt)
    {
        Console.Write(prompt);
        return int.Parse(Console.ReadLine()?.Trim() ?? "0");
    }

    private static double ReadDouble(string prompt)
    {
        Console.Write(prompt);
        return double.Parse(Console.ReadLine()?.Trim() ?? "0");
    }
    
    
}
