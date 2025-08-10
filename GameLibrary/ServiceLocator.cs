using System;
using System.Collections.Generic;

namespace GameLibrary;

/// <summary>
/// Provides a simple service locator pattern implementation for registering and retrieving services by type.
/// This static class acts as a central registry for services, allowing dependency injection without direct references.
/// It uses a dictionary to map service types to their instances.
/// </summary>
public static class ServiceLocator
{
    /// <summary>
    /// The internal dictionary that stores registered services, keyed by their type.
    /// It is readonly to prevent accidental reassignment, but its contents can be modified.
    /// </summary>
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    /// <summary>
    /// Registers a service instance of type T in the locator.
    /// If a service of the same type is already registered, it will be overwritten.
    /// </summary>
    /// <typeparam name="T">The type of the service to register.</typeparam>
    /// <param name="service">The instance of the service to register.</param>
    public static void Register<T>(T service)
    {
        // Store the service instance under its type key
        _services[typeof(T)] = service;
    }

    /// <summary>
    /// Retrieves a registered service instance of type T.
    /// Throws an exception if the service is not registered.
    /// </summary>
    /// <typeparam name="T">The type of the service to retrieve.</typeparam>
    /// <returns>The registered service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no service of type T is registered.</exception>
    public static T Get<T>()
    {
        // Attempt to retrieve the service from the dictionary
        if (_services.TryGetValue(typeof(T), out var service))
        {
            // Cast and return the service if found
            return (T)service;
        }

        // Throw an exception if the service is not found
        throw new InvalidOperationException($"Service {typeof(T).Name} not registered.");
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