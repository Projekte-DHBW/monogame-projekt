using System;
using System.Collections.Generic;

namespace GameLibrary;

/// <summary>
/// Provides a simple service locator pattern implementation for registering and retrieving services by type.
/// This static class acts as a central registry for services, allowing dependency injection without direct references.
/// It uses a dictionary to map service types and optional names to their instances, supporting multiple instances per type.
/// </summary>
public static class ServiceLocator
{
    /// <summary>
    /// The internal dictionary that stores registered services, keyed by a tuple of their type and name.
    /// It is readonly to prevent accidental reassignment, but its contents can be modified.
    /// </summary>
    private static readonly Dictionary<(Type Type, string Name), object> _services = new Dictionary<(Type, string), object>();

    /// <summary>
    /// Registers a service instance of type T in the locator with an optional name.
    /// If a service of the same type and name is already registered, it will be overwritten.
    /// </summary>
    /// <typeparam name="T">The type of the service to register.</typeparam>
    /// <param name="service">The instance of the service to register.</param>
    /// <param name="name">Optional name to distinguish multiple instances of the same type. Defaults to empty string.</param>
    public static void Register<T>(T service, string name = "")
    {
        // Store the service instance under its type and name key
        _services[(typeof(T), name)] = service;
    }

    /// <summary>
    /// Retrieves a registered service instance of type T by optional name.
    /// Throws an exception if the service is not registered.
    /// </summary>
    /// <typeparam name="T">The type of the service to retrieve.</typeparam>
    /// <param name="name">Optional name to distinguish multiple instances of the same type. Defaults to empty string.</param>
    /// <returns>The registered service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no service of type T (and name) is registered.</exception>
    public static T Get<T>(string name = "")
    {
        // Attempt to retrieve the service from the dictionary
        if (_services.TryGetValue((typeof(T), name), out var service))
        {
            // Cast and return the service if found
            return (T)service;
        }

        // Throw an exception if the service is not found
        throw new InvalidOperationException($"Service {typeof(T).Name} (name: '{name}') not registered.");
    }

    /// <summary>
    /// Clears all registered services from the locator.
    /// This resets the dictionary, useful for cleanup or reinitialization.
    /// </summary>
    public static void Clear()
    {
        // Clear the internal dictionary
        _services.Clear();
    }
}