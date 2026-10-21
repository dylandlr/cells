using System;
using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>Package-independent v1 policy boundary: local observations and one motor action.
    /// Submit before the actuator's -60 execution order. No policy owns position or rotation.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CellMotility), typeof(CellFlagellarActuator))]
    public sealed class CellMotorPolicyPort : MonoBehaviour
    {
        public const int ObservationCount = 5;
        public const string SchemaVersion = "cells.motor-policy.v1";
        CellMotility motility;
        CellFlagellarActuator motor;
        CellMetabolism metabolism;
        float previousActivation;
        float previousBehavioralMultiplier;
        bool previousControl;

        void OnEnable()
        {
            motility = GetComponent<CellMotility>();
            motor = GetComponent<CellFlagellarActuator>();
            metabolism = GetComponent<CellMetabolism>();
            previousControl = motility.externalMotorControl;
            previousActivation = motor.motorActivation;
            previousBehavioralMultiplier = motor.behavioralMotorMultiplier;
            motility.externalMotorControl = true;
            motor.motorActivation = 0f;
            motor.behavioralMotorMultiplier = 1f;
        }

        void OnDisable()
        {
            if (motility) motility.externalMotorControl = previousControl;
            if (motor)
            {
                motor.motorActivation = previousActivation;
                motor.behavioralMotorMultiplier = previousBehavioralMultiplier;
            }
        }

        /// <summary>One continuous action in [0,1]. Nonfinite output fails closed.</summary>
        public void ApplyMotorAction(float activation)
        {
            if (!isActiveAndEnabled || !motor) return;
            motor.motorActivation = metabolism && metabolism.alive && Finite(activation)
                ? Mathf.Clamp01(activation) : 0f;
        }

        /// <summary>Energy, local nutrient, local toxin, actual motor activation, integrity.
        /// Positive unbounded quantities use x/(1+x); no world position or gradients.</summary>
        public void ReadObservations(float[] destination)
        {
            if (destination == null || destination.Length != ObservationCount)
                throw new ArgumentException("Expected exactly five observation slots.");
            destination[0] = Compress(metabolism ? metabolism.energy : 0f);
            var field = motility ? motility.field : null;
            destination[1] = Compress(field && !string.IsNullOrEmpty(motility.attractantId)
                ? field.Sample(motility.attractantId, transform.position) : 0f);
            destination[2] = Compress(field && !string.IsNullOrEmpty(motility.repellentId)
                ? field.Sample(motility.repellentId, transform.position) : 0f);
            destination[3] = Unit(motor ? motor.ActualActivation : 0f);
            destination[4] = Unit(motor ? motor.structuralIntegrity : 0f);
        }

        static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        static float Unit(float x) => Finite(x) ? Mathf.Clamp01(x) : 0f;
        static float Compress(float x) => Finite(x) && x > 0f ? 1f - 1f / (1f + x) : 0f;
    }
}
