using System;
using System.Reflection;
using StardewValley;

namespace RealityCheck.Services;

public sealed class WeatherFactorService
{
    public double GetWeatherFactor(string marketCategory)
    {
        string weather = this.GetCurrentWeatherKey();

        return weather switch
        {
            "Rain" => this.GetRainFactor(marketCategory),
            "Storm" => this.GetStormFactor(marketCategory),
            "Snow" => this.GetSnowFactor(marketCategory),
            "GreenRain" => this.GetGreenRainFactor(marketCategory),
            _ => this.GetSunFactor(marketCategory)
        };
    }

    public string GetCurrentWeatherKey()
    {
        if (this.IsGreenRain())
            return "GreenRain";

        if (Game1.isLightning)
            return "Storm";

        if (Game1.isSnowing)
            return "Snow";

        if (Game1.isRaining)
            return "Rain";

        return "Sun";
    }

    private double GetSunFactor(string marketCategory)
    {
        return marketCategory switch
        {
            "Fruit" => 0.95,
            _ => 1.00
        };
    }

    private double GetRainFactor(string marketCategory)
    {
        return marketCategory switch
        {
            "Fish" => 1.05,
            "BuildingResource" => 1.05,
            "Forage" => 1.05,
            "Vegetable" => 0.95,
            "Flower" => 0.95,
            _ => 1.00
        };
    }

    private double GetStormFactor(string marketCategory)
    {
        return marketCategory switch
        {
            "Fish" => 1.08,
            "BuildingResource" => 1.05,
            "Vegetable" => 1.05,
            "Fruit" => 1.05,
            "Flower" => 1.05,
            "Forage" => 1.05,
            _ => 1.00
        };
    }

    private double GetSnowFactor(string marketCategory)
    {
        return marketCategory switch
        {
            "BuildingResource" => 1.05,
            "Forage" => 1.05,
            _ => 1.00
        };
    }

    private double GetGreenRainFactor(string marketCategory)
    {
        return marketCategory switch
        {
            "Vegetable" => 1.10,
            "Flower" => 1.10,
            _ => 1.00
        };
    }

    private bool IsGreenRain()
    {
        try
        {
            MethodInfo? method = typeof(Game1).GetMethod(
                "IsGreenRainingHere",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null
            );

            if (method?.Invoke(null, null) is bool result)
                return result;
        }
        catch
        {
            // Older game versions or non-green-rain contexts can safely fall back to ordinary weather flags.
        }

        try
        {
            FieldInfo? field = typeof(Game1).GetField(
                "isGreenRain",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            );

            if (field?.GetValue(null) is bool result)
                return result;
        }
        catch
        {
            // Ignore and fall back.
        }

        try
        {
            PropertyInfo? property = typeof(Game1).GetProperty(
                "isGreenRain",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            );

            if (property?.GetValue(null) is bool result)
                return result;
        }
        catch
        {
            // Ignore and fall back.
        }

        return false;
    }
}
