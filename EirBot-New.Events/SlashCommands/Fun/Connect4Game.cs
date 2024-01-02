using DisCatSharp;
using DisCatSharp.Entities;

namespace EirBot_New.Events.Connect4;

public class Connect4Game {
	private static string EMPTY = ":white_circle:";
	private static string YELLOW = ":yellow_circle:";
	private static string RED = ":red_circle:";

	private enum Space {
		EMPTY,
		YELLOW,
		RED
	}

	public DiscordClient client;
	public long gameID;
	public DateTimeOffset timestamp;
	public DiscordMessage message;
	private Space[,] board;

	public bool started { get; private set; } = false;
	public bool gameOver { get; private set; } = false;
	public bool draw { get; private set; } = false;
	public bool nextTurnYellow { get; private set; } = true;
	public DiscordUser redPlayer { get; private set; }
	public DiscordUser yellowPlayer { get; private set; }

	public void Init() {
		this.board = new Space[7, 6];
		for (var col = 0; col < 7; col++)
		for (var row = 0; row < 6; row++)
			this.board[col, row] = Space.EMPTY;
	}

	public void SetPlayers(DiscordUser redPlayer, DiscordUser yellowPlayer) {
		this.redPlayer = redPlayer;
		this.yellowPlayer = yellowPlayer;
	}

	private string GetSpaceCharacter(Space space) {
		switch (space) {
			case Space.YELLOW:
				return YELLOW;
			case Space.RED:
				return RED;
			default:
				return EMPTY;
		}
	}

	public DiscordMessageBuilder Display() {
		var description = "";
		description += RED + ": " + this.redPlayer.Mention;
		if (this.gameOver && !this.draw && !this.nextTurnYellow)
			description += " :confetti_ball:";
		description += "\n";
		description += YELLOW + ": " + this.yellowPlayer.Mention;
		if (this.gameOver && !this.draw && this.nextTurnYellow)
			description += " :confetti_ball:";
		description += "\n";
		description += this.DisplayBoard();
		var response = new DiscordMessageBuilder()
			.AddEmbed(new DiscordEmbedBuilder()
				.WithColor(this.nextTurnYellow ? DiscordColor.Yellow : DiscordColor.Red)
				.WithAuthor(this.nextTurnYellow ? this.yellowPlayer.Username : this.redPlayer.Username, null, this.nextTurnYellow ? this.yellowPlayer.AvatarUrl : this.redPlayer.AvatarUrl)
				.WithDescription(description)
				.WithFooter(this.StatusMessage(), this.client.CurrentUser.AvatarUrl)
				.WithTimestamp(this.timestamp)
			);
		return response;
	}

	public int ParseColumn(DiscordEmoji emoji) {
		switch (emoji.GetDiscordName()) {
			case ":one:":
				return 0;
			case ":two:":
				return 1;
			case ":three:":
				return 2;
			case ":four:":
				return 3;
			case ":five:":
				return 4;
			case ":six:":
				return 5;
			case ":seven:":
				return 6;

			default:
				break;
		}

		return -1;
	}

	private void Eval(int col, int row) {
		var color = this.board[col, row];
		this.gameOver = this.EvalHorizontals(col, row, color);
		if (!this.gameOver)
			this.gameOver = this.EvalVerticals(col, row, color);
		if (!this.gameOver)
			this.gameOver = this.EvalDiagonals(col, row, color);
	}

	private bool EvalHorizontals(int col, int row, Space color) {
		var matchingCount = 0;
		for (var i = col - 3; i <= col + 3; i++)
			if (i > -1 && i < 7 && this.board[i, row] == color) {
				matchingCount++;
				if (matchingCount == 4)
					return true;
			} else
				matchingCount = 0;

		return false;
	}

	private bool EvalVerticals(int col, int row, Space color) {
		var matchingCount = 0;
		for (var i = row - 3; i <= row + 3; i++)
			if (i > -1 && i < 6 && this.board[col, i] == color) {
				matchingCount++;
				if (matchingCount == 4)
					return true;
			} else
				matchingCount = 0;

		return false;
	}

	private bool EvalDiagonals(int col, int row, Space color) {
		var matchingCount = 0;
		for (var i = -3; i <= 3; i++)
			if (col + i > -1 && row + i > -1 && col + i < 7 && row + i < 6 && this.board[col + i, row + i] == color) {
				matchingCount++;
				if (matchingCount == 4)
					return true;
			} else
				matchingCount = 0;

		matchingCount = 0;
		for (var i = -3; i <= 3; i++)
			if (col + i > -1 && row - i > -1 && col + i < 7 && row - i < 6 && this.board[col + i, row - i] == color) {
				matchingCount++;
				if (matchingCount == 4)
					return true;
			} else
				matchingCount = 0;

		return false;
	}

	private void UpdateStatus() {
		this.draw = true;
		for (var i = 0; i <= 6; i++)
			if (this.ColumnFree(i)) {
				this.draw = false;
				break;
			}

		if (this.draw)
			this.gameOver = true;
		if (!this.gameOver)
			this.nextTurnYellow = !this.nextTurnYellow;
	}

	private string DisplayBoard() {
		var boardString = "";
		for (var row = 5; row >= 0; row--) {
			for (var col = 0; col < 7; col++)
				boardString += this.GetSpaceCharacter(this.board[col, row]);
			if (row > 0)
				boardString += "\n";
		}

		return boardString;
	}

	private string StatusMessage() {
		if (this.gameOver)
			if (!this.draw)
				return string.Format("{0} won!", (this.nextTurnYellow ? this.yellowPlayer : this.redPlayer).Username);
			else
				return "It's a draw.";
		else
			return string.Format("{0}'s turn.", (this.nextTurnYellow ? this.yellowPlayer : this.redPlayer).Username);
	}

	public bool ColumnFree(int col) =>
		this.board[col, 5] == Space.EMPTY;

	public void Place(int col) {
		var color = this.nextTurnYellow ? Space.YELLOW : Space.RED;

		for (var bottomMost = 0; bottomMost < 6; bottomMost++)
			if (this.board[col, bottomMost] == Space.EMPTY) {
				this.board[col, bottomMost] = color;
				this.Eval(col, bottomMost);
				break;
			}

		this.UpdateStatus();
	}
}