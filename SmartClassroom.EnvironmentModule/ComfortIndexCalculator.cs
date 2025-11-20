using System;

namespace SmartClassroom.Modules
{
    /// <summary>
    /// Calculates comfort index based on temperature, humidity, and CO2 levels.
    /// Single Responsibility: Only handles comfort calculations.
    /// </summary>
    public class ComfortIndexCalculator
    {
        // Optimal ranges
        private const double OPTIMAL_TEMP_MIN = 20.0;
        private const double OPTIMAL_TEMP_MAX = 24.0;
        private const double OPTIMAL_HUMIDITY_MIN = 30.0;
        private const double OPTIMAL_HUMIDITY_MAX = 60.0;
        private const double OPTIMAL_CO2_MAX = 1000.0;

        /// <summary>
        ///Calculate comfort index (0-100 scale)
        /// </summary>
        public double Calculate(double temperature, double humidity, double co2)
        {
            double tempScore = CalculateTemperatureScore(temperature);
            double humidityScore = CalculateHumidityScore(humidity);
            double co2Score = CalculateCO2Score(co2);

            // Weighted average: temp 40%, humidity 30%, CO2 30%
            return (tempScore * 0.4) + (humidityScore * 0.3) + (co2Score * 0.3);
        }

        private double CalculateTemperatureScore(double temp)
        {
            if (temp >= OPTIMAL_TEMP_MIN && temp <= OPTIMAL_TEMP_MAX)
                return 100.0;

            if (temp < OPTIMAL_TEMP_MIN)
            {
                double deviation = OPTIMAL_TEMP_MIN - temp;
                return Math.Max(0, 100.0 - (deviation * 12.0)); // increased penalty
            }

            double deviationAbove = temp - OPTIMAL_TEMP_MAX;
            return Math.Max(0, 100.0 - (deviationAbove * 12.0)); // increased penalty
        }

        private double CalculateHumidityScore(double humidity)
        {
            if (humidity >= OPTIMAL_HUMIDITY_MIN && humidity <= OPTIMAL_HUMIDITY_MAX)
                return 100.0;

            if (humidity < OPTIMAL_HUMIDITY_MIN)
            {
                double deviation = OPTIMAL_HUMIDITY_MIN - humidity;
                return Math.Max(0, 100.0 - (deviation * 2.5)); // stronger penalty
            }

            double deviationAbove = humidity - OPTIMAL_HUMIDITY_MAX;
            return Math.Max(0, 100.0 - (deviationAbove * 2.5)); // stronger penalty
        }

        private double CalculateCO2Score(double co2)
        {
            if (co2 <= OPTIMAL_CO2_MAX)
                return 100.0;

            double excess = co2 - OPTIMAL_CO2_MAX;

            // stronger CO₂ penalty (was 0.1)
            return Math.Max(0, 100.0 - (excess * 0.15));
        }
    }
}