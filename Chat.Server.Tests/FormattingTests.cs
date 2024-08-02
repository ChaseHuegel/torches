using Chat.Server.Types;
using DryIoc;
using Library.Configuration.Localization;
using Library.Types;
using Networking;
using Packets.Chat;
using SmartFormat;
using SmartFormat.Extensions;

namespace Chat.Server.Tests;

public class FormattingTests : TestBase
{
    protected override void SetupContainer(Container container)
    {
        container.Register<SessionService>(Reuse.Singleton);
    }

    protected override void OnSetup()
    {
        var enLang = new Language("en");
        enLang.Translations.Add("Chat.Format.Self", "{Value}");
        enLang.Translations.Add("Chat.Format.Other", "{Sender} {Value}");

        var languages = new Language[] {
            enLang
        };

        Smart.Default.Settings.Localization.LocalizationProvider = new LocalizationService(languages);
        Smart.Default.AddExtensions(new LocalizationFormatter());
    }

    protected override void OnTearDown()
    {
        Smart.Default.Settings.Localization.LocalizationProvider = null;
    }

    [Test]
    public void ChatFormatResolves()
    {
        var sessionService = Container.Resolve<SessionService>();

        const string message = "hello world!";
        Session sender = sessionService.RequestNew().Value;
        Session target = sessionService.RequestNew().Value;

        var chatPacket = new ChatPacket(0, ChatChannel.Whisper, target.ID, message);
        var chatMessage = new ChatMessage((int)chatPacket.Channel, sender, target, chatPacket.Value);

        string formattedSelf = Smart.Format("{:L:Chat.Format.Self}", chatMessage);
        string formattedOther = Smart.Format("{:L:Chat.Format.Other}", chatMessage);

        Assert.That(formattedSelf, Is.EqualTo(message));
        Assert.That(formattedOther, Is.EqualTo($"{sender} {message}"));
    }
}