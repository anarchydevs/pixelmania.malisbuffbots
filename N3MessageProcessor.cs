using AOSharp.Clientless;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MalisBuffBots
{
    public class N3MessageProcessor
    {
        private QueueProcessor _queueProcessor;

        public N3MessageProcessor(QueueProcessor queueProcessor)
        {
            _queueProcessor = queueProcessor;
            Client.MessageReceived += OnMessageReceived;
        }

        public void OnMessageReceived(object _, Message msg)
        {
            if (msg.Header.PacketType != PacketType.N3Message)
                return;

            N3Message n3Msg = (N3Message)msg.Body;

            switch (n3Msg.N3MessageType)
            {
                case N3MessageType.CharacterAction:
                    ProcessCharacterActionMessage((CharacterActionMessage)n3Msg);
                    break;
                case N3MessageType.Feedback:
                    ProcessFeedbackMessage((FeedbackMessage)n3Msg);
                    break;
                case N3MessageType.TeamMember:
                    OnTeamMemberMessage((TeamMemberMessage)n3Msg);
                    break;
            }
        }

        private void ProcessCharacterActionMessage(CharacterActionMessage actionMsg)
        {
            switch (actionMsg.Action)
            {
                case CharacterActionType.AcceptTeamRequest:
                    Logger.Information($"Team invite accepted from '{actionMsg.Target.Instance}'");
                    break;
                case CharacterActionType.TeamRequest:
                    OnTeamRequestAction(actionMsg.Identity, actionMsg.Target);
                    break;
                case CharacterActionType.FinishNanoCasting:
                    OnFinishNanoCastingAction(actionMsg.Identity, actionMsg.Target, actionMsg.Parameter2);
                    break;
                case CharacterActionType.TeamMemberLeft:
                    if (actionMsg.Target == DynelManager.LocalPlayer.Identity)
                        Main.Ipc.BotCache.BroadcastTeamInfoMessage();
                    break;
                case CharacterActionType.SetNanoDuration:
                    OnSetNanoDurationAction(actionMsg.Identity,actionMsg.Target.Instance);
                    break;
            }
        }

        private void OnSetNanoDurationAction(Identity identity, int nanoId)
        {
            if (identity != DynelManager.LocalPlayer.Identity)
                return;

            if (Main.RebuffProcessor.Contains(nanoId, out _))
                return;

            DynelManager.LocalPlayer.ForceRemoveBuff(nanoId);    
        }

        internal static void OnTeamMemberMessage(TeamMemberMessage teamMsg)
        {
            Main.Ipc.BotCache.BroadcastTeamInfoMessage(teamMsg.Character);
        }

        private void OnTeamRequestAction(Identity identity, Identity target)
        {
            Logger.Information($"Team request received from '{identity.Instance}'");

            if (Main.Ipc.BotCache.Entries.Values.Any(x => x.Identity == target))
            {
                Logger.Information("accepting team invite");
                Team.Accept(target);
                _queueProcessor.TeamTimeout.Reset();
            }
        }

        private void OnFinishNanoCastingAction(Identity identity, Identity target, int param2)
        {
            if (identity != DynelManager.LocalPlayer.Identity)
                return;

            if (_queueProcessor.Queue.Current == null)
                return;

            if (!_queueProcessor.Queue.Current.NanoEntry.ContainsId(param2))
                return;

            var buffTarget = DynelManager.Players.FirstOrDefault(x => x.Identity == _queueProcessor.Queue.Current.Requester);
            var buffTargetName = buffTarget != null ? buffTarget.Name : target.Instance.ToString();

            Logger.Information($"Finished casting '{_queueProcessor.Queue.Current.NanoEntry.Name}' on '{buffTargetName}'");
            _queueProcessor.ResetCurrentBuffEntry();
        }

        private void ProcessFeedbackMessage(FeedbackMessage feedbackMsg)
        {
            if (feedbackMsg.Identity != DynelManager.LocalPlayer.Identity)
                return;

            if (feedbackMsg.CategoryId != 110)
                return;

            switch ((LdbFeedback)feedbackMsg.MessageId)
            {
                case LdbFeedback.NotEnoughNcu:
                case LdbFeedback.NotInLineOfSight:
                case LdbFeedback.OutOfRange:
                case LdbFeedback.UnableToUseNano:
                case LdbFeedback.BetterNanoInNcu:
                    _queueProcessor.ResetCurrentBuffEntry((LdbFeedback)feedbackMsg.MessageId);
                    break;
                case LdbFeedback.NotEnoughNano:
                    OnNotEnoughNanoFeedback((LdbFeedback)feedbackMsg.MessageId);
                    break;
                default:
                    Logger.Information($"Unregistered ldbfeedback msg:{feedbackMsg.MessageId}");
                    break;
            }
        }

        private void OnNotEnoughNanoFeedback(LdbFeedback messageId)
        {
            if (DynelManager.LocalPlayer.CanUseSitKit(out Item item))
            {
                var moveComponent = DynelManager.LocalPlayer.MovementComponent;
                moveComponent.ChangeMovement(MovementAction.SwitchToSit);
                item.Use();
                moveComponent.ChangeMovement(MovementAction.LeaveSit);
                return;
            }

            _queueProcessor.ResetCurrentBuffEntry(messageId);
        }

    }
}