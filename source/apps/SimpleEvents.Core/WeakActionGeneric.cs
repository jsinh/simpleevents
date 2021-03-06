﻿// ************************************************************************************
// <copyright file="WeakActionGeneric.cs" company="GalaSoft Laurent Bugnion">
// Copyright © GalaSoft Laurent Bugnion 2009-2014
// </copyright>
// ************************************************************************************
// <author>Laurent Bugnion</author>
// <email>laurent@galasoft.ch</email>
// <date>18.9.2009</date>
// <project>GalaSoft.MvvmLight</project>
// <web>http://www.galasoft.ch</web>
// <license>
// The work is originally forked / taken from GalaSoft.MvvmLight toolkit codebase 
// which is licensed under MIT license and remains copyright work of Laurent Bugnion.
// See license.txt in this project or visit http://www.galasoft.ch/license_MIT.txt
// </license>
// ************************************************************************************

namespace DotNetStuffs.SimpleEvents
{
    #region Namespace

    using System;
    using System.Diagnostics.CodeAnalysis;
    
    #endregion

    /// <summary>
    /// Stores an Action without causing a hard reference to be created to the Action's owner. The owner can be garbage collected at any time.
    /// </summary>
    /// <typeparam name="T">The type of the Action's parameter.</typeparam>
    public class WeakAction<T> : WeakAction, IExecuteWithObject
    {
        #region Variable declaration

        /// <summary>
        /// Instance of static action.
        /// </summary>
        private Action<T> staticAction;
        
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the WeakAction class.
        /// </summary>
        /// <param name="action">The action that will be associated to this instance.</param>
        public WeakAction(Action<T> action)
            : this(null == action ? null : action.Target, action)
        {
        }

        /// <summary>
        /// Initializes a new instance of the WeakAction class.
        /// </summary>
        /// <param name="target">The action's owner.</param>
        /// <param name="action">The action that will be associated to this instance.</param>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1", Justification = "Method should fail with an exception if action is null.")]
        public WeakAction(object target, Action<T> action)
        {
            if (action.Method.IsStatic)
            {
                this.staticAction = action;
                if (null != target)
                {
                    // Keep a reference to the target to control the WeakAction's lifetime.
                    this.Reference = new WeakReference(target);
                }

                return;
            }

            this.Method = action.Method;
            this.ActionReference = new WeakReference(action.Target);
            this.Reference = new WeakReference(target);
        }
        
        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of the method that this WeakAction represents.
        /// </summary>
        public override string MethodName
        {
            get
            {
                return null != this.staticAction ? this.staticAction.Method.Name : this.Method.Name;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Action's owner is still alive, or if it was collected by the Garbage Collector already.
        /// </summary>
        public override bool IsAlive
        {
            get
            {
                if (null == this.staticAction && null == this.Reference)
                {
                    return false;
                }

                if (null == this.staticAction)
                {
                    return this.Reference.IsAlive;
                }

                return null == this.Reference || this.Reference.IsAlive;
            }
        }
        
        #endregion

        #region Methods

        /// <summary>
        /// Executes the action. This only happens if the action's owner is still alive.
        /// </summary>
        public new void Execute()
        {
            // ReSharper disable once IntroduceOptionalParameters.Global
            this.Execute(default(T));
        }

        /// <summary>
        /// Executes the action. This only happens if the action's owner is still alive.
        /// </summary>
        /// <param name="parameter">A parameter to be passed to the action.</param>
        public void Execute(T parameter)
        {
            if (null != this.staticAction)
            {
                this.staticAction(parameter);
                return;
            }

            var actionTarget = ActionTarget;

            if (!this.IsAlive)
            {
                return;
            }

            if (null != this.Method && null != this.ActionReference && null != actionTarget)
            {
                this.Method.Invoke(actionTarget, new object[] { parameter });
            }
        }

        /// <summary>
        /// Executes the action with a parameter of type object.
        /// This parameter will be casted to T.
        /// This method implements <see cref="IExecuteWithObject.ExecuteWithObject" /> and can be useful
        /// if you store multiple WeakAction{T} instances but don't know in advance what type T represents.
        /// </summary>
        /// <param name="parameter">The parameter that will be passed to the action after being casted to T.</param>
        public void ExecuteWithObject(object parameter)
        {
            var parameterCasted = (T)parameter;
            this.Execute(parameterCasted);
        }

        /// <summary>
        /// Sets all the actions that this WeakAction contains to null,
        /// which is a signal for containing objects that this WeakAction should be deleted.
        /// </summary>
        public new void MarkForDeletion()
        {
            this.staticAction = null;
            base.MarkForDeletion();
        }
        
        #endregion
    }
}