using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Extensions.Logging;
using ClubActivity;

var game = new GameRoot();
game.Run();

internal class GameRoot : Game
{
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch = null!;
    private ILogger _logger;
    private IConnection _connection;

    public GameRoot() : base()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        spriteBatch = new SpriteBatch(GraphicsDevice);

        _connection = new Pantry();
        _connection.Host();
    }

    protected override void Update(GameTime gameTime)
    {
    }

    protected override void Draw(GameTime gameTime)
    {
        spriteBatch.Begin();
        spriteBatch.End();
    }
}