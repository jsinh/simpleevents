﻿// **************************************************************************
// <copyright file="Messenger.cs" company="Jsinh.in">
// Copyright © Jsinh 2014
// </copyright>
// ****************************************************************************
// <author>Jaspalsinh Chauhan</author>
// <email>jachauhan@gmail.com</email>
// <date>25.01.2014</date>
// <project>Jsinh.Messaging</project>
// <web>http://jsinh.in</web>
// <license>
// See license.txt in this project or http://http://jsinh.in/License-MIT.txt
// </license>
// ****************************************************************************

namespace DotNetStuffs.SimpleEvents
{
    #region Namespace

    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Windows.Threading;

    #endregion

    /// <summary>
    /// The Messenger is a class allowing objects to exchange messages.
    /// </summary>
    public class Messenger : IMessenger
    {
        #region Variable declaration

        /// <summary>
        /// Messenger creation lock instance.
        /// </summary>
        private static readonly object CreationLock = new object();

        /// <summary>
        /// Default instance of messenger.
        /// </summary>
        private static IMessenger defaultInstance;

        /// <summary>
        /// Messenger register lock instance.
        /// </summary>
        private readonly object registerLock = new object();

        /// <summary>
        /// Sub classes action recipients.
        /// </summary>
        private Dictionary<Type, List<WeakActionAndToken>> recipientsOfSubclassesAction;

        /// <summary>
        /// Strict action recipients.
        /// </summary>
        private Dictionary<Type, List<WeakActionAndToken>> recipientsStrictAction;

        /// <summary>
        /// Indicates a value whether clean-up registered or not.
        /// </summary>
        private bool isCleanupRegistered;

        #endregion

        /// <summary>
        /// Gets the Messenger's default instance, allowing
        /// to register and send messages in a static manner.
        /// </summary>
        public static IMessenger Default
        {
            get
            {
                if (null != defaultInstance)
                {
                    return defaultInstance;
                }

                lock (CreationLock)
                {
                    defaultInstance = new Messenger();
                }

                return defaultInstance;
            }
        }

        /// <summary>
        /// Provides a way to override the Messenger.Default instance with
        /// a custom instance, for example for unit testing purposes.
        /// </summary>
        /// <param name="newMessenger">The instance that will be used as Messenger.Default.</param>
        public static void OverrideDefault(IMessenger newMessenger)
        {
            defaultInstance = newMessenger;
        }

        /// <summary>
        /// Sets the Messenger's default (static) instance to null.
        /// </summary>
        public static void Reset()
        {
            defaultInstance = null;
        }

        /// <summary>
        /// Registers a recipient for a type of message TMessage. The action
        /// parameter will be executed when a corresponding message is sent.
        /// <para>Registering a recipient does not create a hard reference to it,
        /// so if this recipient is deleted, no memory leak is caused.</para>
        /// </summary>
        /// <typeparam name="TMessage">The type of message that the recipient registers
        /// for.</typeparam>
        /// <param name="recipient">The recipient that will receive the messages.</param>
        /// <param name="action">The action that will be executed when a message
        /// of type TMessage is sent.</param>
        public virtual void Register<TMessage>(object recipient, Action<TMessage> action)
        {
            Register(recipient, null, false, action);
        }

        /// <summary>
        /// Registers a recipient for a type of message TMessage.
        /// The action parameter will be executed when a corresponding 
        /// message is sent. See the receiveDerivedMessagesToo parameter
        /// for details on how messages deriving from TMessage (or, if TMessage is an interface,
        /// messages implementing TMessage) can be received too.
        /// <para>Registering a recipient does not create a hard reference to it,
        /// so if this recipient is deleted, no memory leak is caused.</para>
        /// </summary>
        /// <typeparam name="TMessage">The type of message that the recipient registers
        /// for.</typeparam>
        /// <param name="recipient">The recipient that will receive the messages.</param>
        /// <param name="receiveDerivedMessagesToo">If true, message types deriving from
        /// TMessage will also be transmitted to the recipient. For example, if a SendOrderMessage
        /// and an ExecuteOrderMessage derive from OrderMessage, registering for OrderMessage
        /// and setting receiveDerivedMessagesToo to true will send SendOrderMessage
        /// and ExecuteOrderMessage to the recipient that registered.
        /// <para>Also, if TMessage is an interface, message types implementing TMessage will also be
        /// transmitted to the recipient. For example, if a SendOrderMessage
        /// and an ExecuteOrderMessage implement IOrderMessage, registering for IOrderMessage
        /// and setting receiveDerivedMessagesToo to true will send SendOrderMessage
        /// and ExecuteOrderMessage to the recipient that registered.</para>
        /// </param>
        /// <param name="action">The action that will be executed when a message
        /// of type TMessage is sent.</param>
        public virtual void Register<TMessage>(object recipient, bool receiveDerivedMessagesToo, Action<TMessage> action)
        {
            Register(recipient, null, receiveDerivedMessagesToo, action);
        }

        /// <summary>
        /// Registers a recipient for a type of message TMessage.
        /// The action parameter will be executed when a corresponding 
        /// message is sent.
        /// <para>Registering a recipient does not create a hard reference to it,
        /// so if this recipient is deleted, no memory leak is caused.</para>
        /// </summary>
        /// <typeparam name="TMessage">The type of message that the recipient registers
        /// for.</typeparam>
        /// <param name="recipient">The recipient that will receive the messages.</param>
        /// <param name="token">A token for a messaging channel. If a recipient registers
        /// using a token, and a sender sends a message using the same token, then this
        /// message will be delivered to the recipient. Other recipients who did not
        /// use a token when registering (or who used a different token) will not
        /// get the message. Similarly, messages sent without any token, or with a different
        /// token, will not be delivered to that recipient.</param>
        /// <param name="action">The action that will be executed when a message
        /// of type TMessage is sent.</param>
        public virtual void Register<TMessage>(object recipient, object token, Action<TMessage> action)
        {
            Register(recipient, token, false, action);
        }

        /// <summary>
        /// Registers a recipient for a type of message TMessage.
        /// The action parameter will be executed when a corresponding 
        /// message is sent. See the receiveDerivedMessagesToo parameter
        /// for details on how messages deriving from TMessage (or, if TMessage is an interface,
        /// messages implementing TMessage) can be received too.
        /// <para>Registering a recipient does not create a hard reference to it,
        /// so if this recipient is deleted, no memory leak is caused.</para>
        /// </summary>
        /// <typeparam name="TMessage">The type of message that the recipient registers
        /// for.</typeparam>
        /// <param name="recipient">The recipient that will receive the messages.</param>
        /// <param name="token">A token for a messaging channel. If a recipient registers
        /// using a token, and a sender sends a message using the same token, then this
        /// message will be delivered to the recipient. Other recipients who did not
        /// use a token when registering (or who used a different token) will not
        /// get the message. Similarly, messages sent without any token, or with a different
        /// token, will not be delivered to that recipient.</param>
        /// <param name="receiveDerivedMessagesToo">If true, message types deriving from
        /// TMessage will also be transmitted to the recipient. For example, if a SendOrderMessage
        /// and an ExecuteOrderMessage derive from OrderMessage, registering for OrderMessage
        /// and setting receiveDerivedMessagesToo to true will send SendOrderMessage
        /// and ExecuteOrderMessage to the recipient that registered.
        /// <para>Also, if TMessage is an interface, message types implementing TMessage will also be
        /// transmitted to the recipient. For example, if a SendOrderMessage
        /// and an ExecuteOrderMessage implement IOrderMessage, registering for IOrderMessage
        /// and setting receiveDerivedMessagesToo to true will send SendOrderMessage
        /// and ExecuteOrderMessage to the recipient that registered.</para>
        /// </param>
        /// <param name="action">The action that will be executed when a message
        /// of type TMessage is sent.</param>
        public virtual void Register<TMessage>(object recipient, object token, bool receiveDerivedMessagesToo, Action<TMessage> action)
        {
            lock (this.registerLock)
            {
                var messageType = typeof(TMessage);

                Dictionary<Type, List<WeakActionAndToken>> recipients;

                if (receiveDerivedMessagesToo)
                {
                    if (this.recipientsOfSubclassesAction == null)
                    {
                        this.recipientsOfSubclassesAction = new Dictionary<Type, List<WeakActionAndToken>>();
                    }

                    recipients = this.recipientsOfSubclassesAction;
                }
                else
                {
                    if (this.recipientsStrictAction == null)
                    {
                        this.recipientsStrictAction = new Dictionary<Type, List<WeakActionAndToken>>();
                    }

                    recipients = this.recipientsStrictAction;
                }

                lock (recipients)
                {
                    List<WeakActionAndToken> list;

                    if (!recipients.ContainsKey(messageType))
                    {
                        list = new List<WeakActionAndToken>();
                        recipients.Add(messageType, list);
                    }
                    else
                    {
                        list = recipients[messageType];
                    }

                    var weakAction = new WeakAction<TMessage>(recipient, action);

                    var item = new WeakActionAndToken
                    {
                        Action = weakAction,
                        Token = token
                    };

                    list.Add(item);
                }
            }

            this.RequestCleanup();
        }

        /// <summary>
        /// Sends a message to registered recipients. The message will
        /// reach all recipients that registered for this message type
        /// using one of the Register methods.
        /// </summary>
        /// <typeparam name="TMessage">The type of message that will be sent.</typeparam>
        /// <param name="message">The message to send to registered recipients.</param>
        public virtual void Send<TMessage>(TMessage message)
        {
            SendToTargetOrType(message, null, null);
        }

        /// <summary>
        /// Sends a message to registered recipients. The message will
        /// reach only recipients that registered for this message type
        /// using one of the Register methods, and that are
        /// of the targetType.
        /// </summary>
        /// <typeparam name="TMessage">The type of message that will be sent.</typeparam>
        /// <typeparam name="TTarget">The type of recipients that will receive
        /// the message. The message won't be sent to recipients of another type.</typeparam>
        /// <param name="message">The message to send to registered recipients.</param>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "This syntax is more convenient than other alternatives.")]
        public virtual void Send<TMessage, TTarget>(TMessage message)
        {
            SendToTargetOrType(message, typeof(TTarget), null);
        }

        /// <summary>
        /// Sends a message to registered recipients. The message will
        /// reach only recipients that registered for this message type
        /// using one of the Register methods, and that are
        /// of the targetType.
        /// </summary>
        /// <typeparam name="TMessage">The type of message that will be sent.</typeparam>
        /// <param name="message">The message to send to registered recipients.</param>
        /// <param name="token">A token for a messaging channel. If a recipient registers
        /// using a token, and a sender sends a message using the same token, then this
        /// message will be delivered to the recipient. Other recipients who did not
        /// use a token when registering (or who used a different token) will not
        /// get the message. Similarly, messages sent without any token, or with a different
        /// token, will not be delivered to that recipient.</param>
        public virtual void Send<TMessage>(TMessage message, object token)
        {
            SendToTargetOrType(message, null, token);
        }

        /// <summary>
        /// Unregisters a messenger recipient completely. After this method
        /// is executed, the recipient will not receive any messages anymore.
        /// </summary>
        /// <param name="recipient">The recipient that must be unregistered.</param>
        public virtual void Unregister(object recipient)
        {
            UnregisterFromLists(recipient, this.recipientsOfSubclassesAction);
            UnregisterFromLists(recipient, this.recipientsStrictAction);
        }

        /// <summary>
        /// Unregisters a message recipient for a given type of messages only. 
        /// After this method is executed, the recipient will not receive messages
        /// of type TMessage anymore, but will still receive other message types (if it
        /// registered for them previously).
        /// </summary>
        /// <param name="recipient">The recipient that must be unregistered.</param>
        /// <typeparam name="TMessage">The type of messages that the recipient wants
        /// to unregister from.</typeparam>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "This syntax is more convenient than other alternatives.")]
        public virtual void Unregister<TMessage>(object recipient)
        {
            this.Unregister<TMessage>(recipient, null, null);
        }

        /// <summary>
        /// Unregisters a message recipient for a given type of messages only and for a given token. 
        /// After this method is executed, the recipient will not receive messages
        /// of type TMessage anymore with the given token, but will still receive other message types
        /// or messages with other tokens (if it registered for them previously).
        /// </summary>
        /// <param name="recipient">The recipient that must be unregistered.</param>
        /// <param name="token">The token for which the recipient must be unregistered.</param>
        /// <typeparam name="TMessage">The type of messages that the recipient wants
        /// to unregister from.</typeparam>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "This syntax is more convenient than other alternatives.")]
        public virtual void Unregister<TMessage>(object recipient, object token)
        {
            this.Unregister<TMessage>(recipient, token, null);
        }

        /// <summary>
        /// Unregisters a message recipient for a given type of messages and for
        /// a given action. Other message types will still be transmitted to the
        /// recipient (if it registered for them previously). Other actions that have
        /// been registered for the message type TMessage and for the given recipient (if
        /// available) will also remain available.
        /// </summary>
        /// <typeparam name="TMessage">The type of messages that the recipient wants
        /// to unregister from.</typeparam>
        /// <param name="recipient">The recipient that must be unregistered.</param>
        /// <param name="action">The action that must be unregistered for
        /// the recipient and for the message type TMessage.</param>
        public virtual void Unregister<TMessage>(object recipient, Action<TMessage> action)
        {
            this.Unregister(recipient, null, action);
        }

        /// <summary>
        /// Unregisters a message recipient for a given type of messages, for
        /// a given action and a given token. Other message types will still be transmitted to the
        /// recipient (if it registered for them previously). Other actions that have
        /// been registered for the message type TMessage, for the given recipient and other tokens (if
        /// available) will also remain available.
        /// </summary>
        /// <typeparam name="TMessage">The type of messages that the recipient wants
        /// to unregister from.</typeparam>
        /// <param name="recipient">The recipient that must be unregistered.</param>
        /// <param name="token">The token for which the recipient must be unregistered.</param>
        /// <param name="action">The action that must be unregistered for
        /// the recipient and for the message type TMessage.</param>
        public virtual void Unregister<TMessage>(object recipient, object token, Action<TMessage> action)
        {
            UnregisterFromLists(recipient, token, action, this.recipientsStrictAction);
            UnregisterFromLists(recipient, token, action, this.recipientsOfSubclassesAction);
            this.RequestCleanup();
        }

        /// <summary>
        /// Provides a non-static access to the static <see cref="Reset"/> method.
        /// Sets the Messenger's default (static) instance to null.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Non static access is needed.")]
        public void ResetAll()
        {
            Reset();
        }

        /// <summary>
        /// Notifies the Messenger that the lists of recipients should
        /// be scanned and cleaned up.
        /// Since recipients are stored as <see cref="WeakReference"/>, 
        /// recipients can be garbage collected even though the Messenger keeps 
        /// them in a list. During the cleanup operation, all "dead"
        /// recipients are removed from the lists. Since this operation
        /// can take a moment, it is only executed when the application is
        /// idle. For this reason, a user of the Messenger class should use
        /// <see cref="RequestCleanup"/> instead of forcing one with the 
        /// <see cref="Cleanup" /> method.
        /// </summary>
        public void RequestCleanup()
        {
            if (!this.isCleanupRegistered)
            {
                Action cleanupAction = this.Cleanup;
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    cleanupAction,
                    DispatcherPriority.ApplicationIdle,
                    null);
                this.isCleanupRegistered = true;
            }
        }

        /// <summary>
        /// Scans the recipients' lists for "dead" instances and removes them.
        /// Since recipients are stored as <see cref="WeakReference"/>, 
        /// recipients can be garbage collected even though the Messenger keeps 
        /// them in a list. During the cleanup operation, all "dead"
        /// recipients are removed from the lists. Since this operation
        /// can take a moment, it is only executed when the application is
        /// idle. For this reason, a user of the Messenger class should use
        /// <see cref="RequestCleanup"/> instead of forcing one with the 
        /// <see cref="Cleanup" /> method.
        /// </summary>
        public void Cleanup()
        {
            CleanupList(this.recipientsOfSubclassesAction);
            CleanupList(this.recipientsStrictAction);
            this.isCleanupRegistered = false;
        }

        /// <summary>
        /// Clean-up list of type - weak action and token.
        /// </summary>
        /// <param name="lists">Dictionary list of type - weak action and token.</param>
        private static void CleanupList(IDictionary<Type, List<WeakActionAndToken>> lists)
        {
            if (null == lists)
            {
                return;
            }

            lock (lists)
            {
                var listsToRemove = new List<Type>();
                foreach (var list in lists)
                {
                    var recipientsToRemove = list.Value
                        .Where(item => item.Action == null || !item.Action.IsAlive)
                        .ToList();

                    foreach (var recipient in recipientsToRemove)
                    {
                        list.Value.Remove(recipient);
                    }

                    if (list.Value.Count == 0)
                    {
                        listsToRemove.Add(list.Key);
                    }
                }

                foreach (var key in listsToRemove)
                {
                    lists.Remove(key);
                }
            }
        }

        /// <summary>
        /// List of weak action and token instance to send message.
        /// </summary>
        /// <typeparam name="TMessage">Type of message to send.</typeparam>
        /// <param name="message">Message to send.</param>
        /// <param name="weakActionsAndTokens">List of weak action and token</param>
        /// <param name="messageTargetType">Message target type.</param>
        /// <param name="token">Message registration token.</param>
        private static void SendToList<TMessage>(TMessage message, IEnumerable<WeakActionAndToken> weakActionsAndTokens, Type messageTargetType, object token)
        {
            if (weakActionsAndTokens == null)
            {
                return;
            }

            //// Clone to protect from people registering in a "receive message" method
            //// Correction Messaging BL0004.007
            var list = weakActionsAndTokens.ToList();
            var listClone = list.Take(list.Count()).ToList();

            foreach (var executeAction in
                from item in listClone
                let executeAction = item.Action as IExecuteWithObject
                where
                    executeAction != null && item.Action.IsAlive && item.Action.Target != null
                    && (
                           messageTargetType == null
                           || item.Action.Target.GetType() == messageTargetType
                           || messageTargetType.IsInstanceOfType(item.Action.Target))
                    && (
                           (item.Token == null && token == null)
                           || (item.Token != null && item.Token.Equals(token)))
                select executeAction)
            {
                executeAction.ExecuteWithObject(message);
            }
        }

        /// <summary>
        /// Unregister recipient from list.
        /// </summary>
        /// <param name="recipient">Instance of recipient.</param>
        /// <param name="lists">List of weak action and token.</param>
        private static void UnregisterFromLists(object recipient, Dictionary<Type, List<WeakActionAndToken>> lists)
        {
            if (recipient == null
                || lists == null
                || lists.Count == 0)
            {
                return;
            }

            lock (lists)
            {
                foreach (var messageType in lists.Keys)
                {
                    foreach (var item in lists[messageType])
                    {
                        var weakAction = (IExecuteWithObject)item.Action;

                        if (weakAction != null
                            && recipient == weakAction.Target)
                        {
                            weakAction.MarkForDeletion();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Unregister from list.
        /// </summary>
        /// <typeparam name="TMessage">Type of message.</typeparam> 
        /// <param name="recipient">Instance of recipient.</param>
        /// <param name="token">Message registration token.</param>
        /// <param name="action">Instance of action of type message.</param>
        /// <param name="lists">List of weak action and token.</param>
        private static void UnregisterFromLists<TMessage>(
            object recipient,
            object token,
            Action<TMessage> action,
            Dictionary<Type, List<WeakActionAndToken>> lists)
        {
            var messageType = typeof(TMessage);

            if (recipient == null
                || lists == null
                || lists.Count == 0
                || !lists.ContainsKey(messageType))
            {
                return;
            }

            lock (lists)
            {
                foreach (var item in lists[messageType])
                {
                    var weakActionCasted = item.Action as WeakAction<TMessage>;

                    if (weakActionCasted != null
                        && recipient == weakActionCasted.Target
                        && (action == null
#if NETFX_CORE
                            || action.GetMethodInfo().Name == weakActionCasted.MethodName)
#else
 || action.Method.Name == weakActionCasted.MethodName)
#endif
 && (token == null
                            || token.Equals(item.Token)))
                    {
                        item.Action.MarkForDeletion();
                    }
                }
            }
        }

        /// <summary>
        /// Send to target or type.
        /// </summary>
        /// <typeparam name="TMessage">Type of message.</typeparam>
        /// <param name="message">Message to send to target or type.</param>
        /// <param name="messageTargetType">Message target type.</param>
        /// <param name="token">Message registration token.</param>
        private void SendToTargetOrType<TMessage>(TMessage message, Type messageTargetType, object token)
        {
            var messageType = typeof(TMessage);

            if (this.recipientsOfSubclassesAction != null)
            {
                // Clone to protect from people registering in a "receive message" method
                // Correction Messaging BL0008.002
                var listClone =
                    this.recipientsOfSubclassesAction.Keys.Take(this.recipientsOfSubclassesAction.Count()).ToList();

                foreach (var type in listClone)
                {
                    List<WeakActionAndToken> list = null;

                    if (messageType == type
#if NETFX_CORE
                        || messageType.GetTypeInfo().IsSubclassOf(type)
                        || type.GetTypeInfo().IsAssignableFrom(messageType.GetTypeInfo()))
#else
 || messageType.IsSubclassOf(type)
                        || type.IsAssignableFrom(messageType))
#endif
                    {
                        lock (this.recipientsOfSubclassesAction)
                        {
                            list = this.recipientsOfSubclassesAction[type].Take(this.recipientsOfSubclassesAction[type].Count()).ToList();
                        }
                    }

                    SendToList(message, list, messageTargetType, token);
                }
            }

            if (this.recipientsStrictAction != null)
            {
                List<WeakActionAndToken> list = null;

                lock (this.recipientsStrictAction)
                {
                    if (this.recipientsStrictAction.ContainsKey(messageType))
                    {
                        list = this.recipientsStrictAction[messageType]
                            .Take(this.recipientsStrictAction[messageType].Count())
                            .ToList();
                    }
                }

                if (list != null)
                {
                    SendToList(message, list, messageTargetType, token);
                }
            }

            this.RequestCleanup();
        }

        /// <summary>
        /// Weak action and token structure.
        /// </summary>
        private struct WeakActionAndToken
        {
            /// <summary>
            /// Instance of weak action.
            /// </summary>
            public WeakAction Action;

            /// <summary>
            /// Instance of token used for registration.
            /// </summary>
            public object Token;
        }
    }
}