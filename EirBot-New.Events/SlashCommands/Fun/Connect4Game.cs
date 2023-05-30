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
		board = new Space[7, 6];
		for (int col = 0; col < 7; col++)
			for (int row = 0; row < 6; row++)
				board[col, row] = Space.EMPTY;
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
		string description = "";
		description += RED + ": " + redPlayer.Mention;
		if (gameOver && !draw && !nextTurnYellow)
			description += " :confetti_ball:";
		description += "\n";
		description += YELLOW + ": " + yellowPlayer.Mention;
		if (gameOver && !draw && nextTurnYellow)
			description += " :confetti_ball:";
		description += "\n";
		description += DisplayBoard();
		DiscordMessageBuilder response = new DiscordMessageBuilder()
			.AddEmbed(new DiscordEmbedBuilder()
				.WithColor(nextTurnYellow ? DiscordColor.Yellow : DiscordColor.Red)
				.WithAuthor(nextTurnYellow ? yellowPlayer.Username : redPlayer.Username, null, nextTurnYellow ? yellowPlayer.AvatarUrl : redPlayer.AvatarUrl)
				.WithDescription(description)
				.WithFooter(StatusMessage(), client.CurrentUser.AvatarUrl)
				.WithTimestamp(timestamp)
			);
		return response;
	}

	public int ParseColumn(DiscordEmoji emoji) {
		switch(emoji.GetDiscordName()) {
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
		Space color = board[col, row];
		gameOver = EvalHorizontals(col, row, color);
		if (!gameOver)
			gameOver = EvalVerticals(col, row, color);
		if (!gameOver)
			gameOver = EvalDiagonals(col, row, color);
	}

	private bool EvalHorizontals(int col, int row, Space color) {
		int matchingCount = 0;
		for (int i = col - 3; i <= col + 3; i++)
			if (i > -1 && i < 7 && board[i, row] == color) {
				matchingCount++;
				if (matchingCount == 4)
					return true;
			} else
				matchingCount = 0;
		return false;
	}

	private bool EvalVerticals(int col, int row, Space color) {
		int matchingCount = 0;
		for (int i = row - 3; i <= row + 3; i++) {
			if (i > -1 && i < 6 && board[col, i] == color) {
				matchingCount++;
				if (matchingCount == 4)
					return true;
			} else
				matchingCount = 0;
		}
		return false;
	}

	private bool EvalDiagonals(int col, int row, Space color) {
		int matchingCount = 0;
		for (int i = -3; i <= 3; i++)
			if (col + i > -1 && row + i > -1 && col + i < 7 && row + i < 6 && board[col + i, row + i] == color) {
				matchingCount++;
				if (matchingCount == 4)
					return true;
			} else
				matchingCount = 0;

		matchingCount = 0;
		for (int i = -3; i <= 3; i++) {
			if (col + i > -1 && row - i > -1 && col + i < 7 && row - i < 6 && board[col + i, row - i] == color) {
				matchingCount++;
				if (matchingCount == 4)
					return true;
			} else
				matchingCount = 0;
		}
		return false;
	}

	private void UpdateStatus() {
		draw = true;
		for (int i = 0; i <= 6; i++)
			if (ColumnFree(i)) {
				draw = false;
				break;
			}
		if (draw)
			gameOver = true;
		if (!gameOver)
			nextTurnYellow = !nextTurnYellow;
	}

	private string DisplayBoard() {
		string boardString = "";
		for (int row = 5; row >= 0; row--) {
			for (int col = 0; col < 7; col++)
				boardString += GetSpaceCharacter(board[col, row]);
			if (row > 0)
				boardString += "\n";
		}
		return boardString;
	}

	private string StatusMessage() {
		if (gameOver)
			if (!draw)
				return String.Format("{0} won!", (nextTurnYellow ? yellowPlayer : redPlayer).Username);
			else
				return "It's a draw.";
		else
			return String.Format("{0}'s turn.", (nextTurnYellow ? yellowPlayer : redPlayer).Username);
	}

	public bool ColumnFree(int col) {
		return board[col, 5] == Space.EMPTY;
	}

	public void Place(int col) {
		Space color = nextTurnYellow ? Space.YELLOW : Space.RED;

		for (int bottomMost = 0; bottomMost < 6; bottomMost++)
			if (board[col, bottomMost] == Space.EMPTY) {
				board[col, bottomMost] = color;
				Eval(col, bottomMost);
				break;
			}

		UpdateStatus();
	}
}
