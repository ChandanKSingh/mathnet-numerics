// <copyright file="ResidualStopCriterium.cs" company="Math.NET">
// Math.NET Numerics, part of the Math.NET Project
// http://numerics.mathdotnet.com
// http://github.com/mathnet/mathnet-numerics
// http://mathnetnumerics.codeplex.com
//
// Copyright (c) 2009-2010 Math.NET
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
// </copyright>

using System;
using System.Diagnostics;
using MathNet.Numerics.LinearAlgebra.Solvers;
using MathNet.Numerics.Properties;

namespace MathNet.Numerics.LinearAlgebra.Complex.Solvers
{

#if NOSYSNUMERICS
    using Complex = Numerics.Complex;
#else
    using Complex = System.Numerics.Complex;
#endif

    /// <summary>
    /// Defines an <see cref="IIterationStopCriterium{T}"/> that monitors residuals as stop criterium.
    /// </summary>
    public sealed class ResidualStopCriterium : IIterationStopCriterium<Complex>
    {
        /// <summary>
        /// The default value for the maximum value of the residual.
        /// </summary>
        public const double DefaultMaximumResidual = 1e-12;

        /// <summary>
        /// The default value for the minimum number of iterations.
        /// </summary>
        public const int DefaultMinimumIterationsBelowMaximum = 0;

        /// <summary>
        /// Defines the default last iteration number. Set to -1 because iterations normally start at 0.
        /// </summary>
        const int DefaultLastIterationNumber = -1;

        /// <summary>
        /// The maximum value for the residual below which the calculation is considered converged.
        /// </summary>
        double _maximum;

        /// <summary>
        /// The minimum number of iterations for which the residual has to be below the maximum before
        /// the calculation is considered converged.
        /// </summary>
        int _minimumIterationsBelowMaximum;

        /// <summary>
        /// The status of the calculation
        /// </summary>
        IterationStatus _status = IterationStatus.Continue;

        /// <summary>
        /// The number of iterations since the residuals got below the maximum.
        /// </summary>
        int _iterationCount;

        /// <summary>
        /// The iteration number of the last iteration.
        /// </summary>
        int _lastIteration = DefaultLastIterationNumber;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResidualStopCriterium"/> class with the specified 
        /// maximum residual and minimum number of iterations.
        /// </summary>
        /// <param name="maximum">
        /// The maximum value for the residual below which the calculation is considered converged.
        /// </param>
        /// <param name="minimumIterationsBelowMaximum">
        /// The minimum number of iterations for which the residual has to be below the maximum before
        /// the calculation is considered converged.
        /// </param>
        public ResidualStopCriterium(double maximum = DefaultMaximumResidual, int minimumIterationsBelowMaximum = DefaultMinimumIterationsBelowMaximum)
        {
            if (maximum < 0)
            {
                throw new ArgumentOutOfRangeException("maximum");
            }

            if (minimumIterationsBelowMaximum < 0)
            {
                throw new ArgumentOutOfRangeException("minimumIterationsBelowMaximum");
            }

            _maximum = maximum;
            _minimumIterationsBelowMaximum = minimumIterationsBelowMaximum;
        }

        /// <summary>
        /// Gets or sets the maximum value for the residual below which the calculation is considered 
        /// converged.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the <c>Maximum</c> is set to a negative value.</exception>
        public double Maximum
        {
            [DebuggerStepThrough]
            get
            {
                return _maximum;
            }

            [DebuggerStepThrough]
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _maximum = value;
            }
        }

        /// <summary>
        /// Returns the maximum residual to the default.
        /// </summary>
        public void ResetMaximumResidualToDefault()
        {
            _maximum = DefaultMaximumResidual;
        }

        /// <summary>
        /// Gets or sets the minimum number of iterations for which the residual has to be
        /// below the maximum before the calculation is considered converged.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the <c>BelowMaximumFor</c> is set to a value less than 1.</exception>
        public int MinimumIterationsBelowMaximum
        {
            [DebuggerStepThrough]
            get
            {
                return _minimumIterationsBelowMaximum;
            }

            [DebuggerStepThrough]
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _minimumIterationsBelowMaximum = value;
            }
        }

        /// <summary>
        /// Returns the minimum number of iterations to the default.
        /// </summary>
        public void ResetMinimumIterationsBelowMaximumToDefault()
        {
            _minimumIterationsBelowMaximum = DefaultMinimumIterationsBelowMaximum;
        }

        /// <summary>
        /// Determines the status of the iterative calculation based on the stop criteria stored
        /// by the current <see cref="IIterationStopCriterium{T}"/>. Result is set into <c>Status</c> field.
        /// </summary>
        /// <param name="iterationNumber">The number of iterations that have passed so far.</param>
        /// <param name="solutionVector">The vector containing the current solution values.</param>
        /// <param name="sourceVector">The right hand side vector.</param>
        /// <param name="residualVector">The vector containing the current residual vectors.</param>
        /// <remarks>
        /// The individual stop criteria may internally track the progress of the calculation based
        /// on the invocation of this method. Therefore this method should only be called if the 
        /// calculation has moved forwards at least one step.
        /// </remarks>
        public IterationStatus DetermineStatus(int iterationNumber, Vector<Complex> solutionVector, Vector<Complex> sourceVector, Vector<Complex> residualVector)
        {
            if (iterationNumber < 0)
            {
                throw new ArgumentOutOfRangeException("iterationNumber");
            }

            if (solutionVector.Count != sourceVector.Count)
            {
                throw new ArgumentException(Resources.ArgumentVectorsSameLength, "sourceVector");
            }

            if (solutionVector.Count != residualVector.Count)
            {
                throw new ArgumentException(Resources.ArgumentVectorsSameLength, "residualVector");
            }

            // Store the infinity norms of both the solution and residual vectors
            // These values will be used to calculate the relative drop in residuals
            // later on.
            var residualNorm = residualVector.InfinityNorm();

            // Check the residuals by calculating:
            // ||r_i|| <= stop_tol * ||b||
            var stopCriterium = ComputeStopCriterium(sourceVector.InfinityNorm());

            // First check that we have real numbers not NaN's.
            // NaN's can occur when the iterative process diverges so we
            // stop if that is the case.
            if (double.IsNaN(stopCriterium) || double.IsNaN(residualNorm))
            {
                _iterationCount = 0;
                _status = IterationStatus.Diverged;
                return _status;
            }

            // ||r_i|| <= stop_tol * ||b||
            // Stop the calculation if it's clearly smaller than the tolerance
            var decimalMagnitude = Math.Abs(stopCriterium.Magnitude()) + 1;
            if (residualNorm.IsSmallerDecimal(stopCriterium, decimalMagnitude))
            {
                if (_lastIteration <= iterationNumber)
                {
                    _iterationCount = iterationNumber - _lastIteration;
                    _status = _iterationCount >= _minimumIterationsBelowMaximum ? IterationStatus.Converged : IterationStatus.Continue;
                }
            }
            else
            {
                _iterationCount = 0;
                _status = IterationStatus.Continue;
            }

            _lastIteration = iterationNumber;
            return _status;
        }

        /// <summary>
        /// Calculate stop criterium
        /// </summary>
        /// <param name="solutionNorm">Solution vector norm</param>
        /// <returns>Criterium value</returns>
        double ComputeStopCriterium(double solutionNorm)
        {
            // This is criterium 1 from Templates for the solution of linear systems.
            // The problem with this criterium is that it's not limiting enough. For now 
            // we won't use it. Later on we might get back to it.
            // return mMaximumResidual * (System.Math.Abs(mMatrixNorm) * System.Math.Abs(solutionNorm) + System.Math.Abs(mVectorNorm));

            // For now use criterium 2 from Templates for the solution of linear systems. See page 60.
            return _maximum*Math.Abs(solutionNorm);
        }

        /// <summary>
        /// Gets the current calculation status.
        /// </summary>
        public IterationStatus Status
        {
            [DebuggerStepThrough]
            get
            {
                return _status;
            }
        }

        /// <summary>
        /// Resets the <see cref="IIterationStopCriterium{T}"/> to the pre-calculation state.
        /// </summary>
        public void Reset()
        {
            _status = IterationStatus.Continue;
            _iterationCount = 0;
            _lastIteration = DefaultLastIterationNumber;
        }

        /// <summary>
        /// Clones the current <see cref="ResidualStopCriterium"/> and its settings.
        /// </summary>
        /// <returns>A new instance of the <see cref="ResidualStopCriterium"/> class.</returns>
        public IIterationStopCriterium<Complex> Clone()
        {
            return new ResidualStopCriterium(_maximum, _minimumIterationsBelowMaximum);
        }
    }
}
