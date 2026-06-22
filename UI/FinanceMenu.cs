using RealityCheck.Services;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework;

namespace RealityCheck.UI;

public class FinanceMenu : IClickableMenu
{
    private readonly LedgerService ledgerService;

    public FinanceMenu(LedgerService ledgerService)
    {
        this.ledgerService = ledgerService;
    }

    public override void draw(Microsoft.Xna.Framework.Graphics.SpriteBatch b)
    {
        base.draw(b);

        IClickableMenu.drawTextureBox(
            b,
            Game1.uiViewport.Width / 2 - 300,
            Game1.uiViewport.Height / 2 - 200,
            600,
            400,
            Color.White
        );

        Utility.drawTextWithShadow(
            b,
            "Reality Check",
            Game1.dialogueFont,
            new Vector2(Game1.uiViewport.Width / 2 - 120, Game1.uiViewport.Height / 2 - 160),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            $"Ledger entries: {this.ledgerService.GetEntries().Count}",
            Game1.smallFont,
            new Vector2(Game1.uiViewport.Width / 2 - 120, Game1.uiViewport.Height / 2 - 90),
            Game1.textColor
        );

        this.drawMouse(b);
    }
}