﻿namespace test_workflow;

/// <summary>
///     A class for child objects.
/// </summary>
public class ChildModel(int age)
{
    public bool Growable { get; set; }

    // Age of the child
    public int Age { get; set; } = age;

    // Salary
    private int Salary { get; set; }

    // Secrets of the child
    private string Secrets { get; set; } = string.Empty;

    // Gender
    public byte Gender { get; set; }
}