$MINESWEEPER_TILE[0] = 11;
$MINESWEEPER_TILE[1] = 10;
$MINESWEEPER_TILE[2] = 9;
$MINESWEEPER_TILE[3] = 8;
$MINESWEEPER_TILE[4] = 7;
$MINESWEEPER_TILE[5] = 6;
$MINESWEEPER_TILE[6] = 5;
$MINESWEEPER_TILE[7] = 4;
$MINESWEEPER_TILE[8] = 3;
$MINESWEEPER_TILE["MINE"] = 0;
$MINESWEEPER_TILE["FLAGGED"] = 1;
$MINESWEEPER_TILE["HIDDEN"] = 2;

exec("./sounds.cs");
exec("./shapes.cs");

function GameConnection::deleteGrid(%this) {
	%w = %this.gridWidth;
	%h = %this.gridHeight;

	for(%x = 0; %x < %w; %x++) {
		for(%y = 0; %y < %h; %y++) {
			%this.gridBrick[%x,%y].delete();
		}
	}

	if(isObject($Server::Minesweeper::GridFloor[%this.gridIdx])) {
		$Server::Minesweeper::GridFloor[%this.gridIdx].delete();
	}

	$Server::Minesweeper::GridSpot[%this.gridIdx] = 0;
}

function GameConnection::setSurroundingMines(%this) {
	%width = %this.gridWidth;
	%height = %this.gridHeight;

	for(%x = 0; %x < %width; %x++) {
		for(%y = 0; %y < %height; %y++) {
			%brick = %this.gridBrick[%x, %y];
			%brick.surroundingMines += %this.gridBrick[%x - 1, %y - 1].isMine; // :^)
			%brick.surroundingMines += %this.gridBrick[%x - 1, %y].isMine;
			%brick.surroundingMines += %this.gridBrick[%x - 1, %y + 1].isMine;
			%brick.surroundingMines += %this.gridBrick[%x, %y - 1].isMine;
			%brick.surroundingMines += %this.gridBrick[%x, %y + 1].isMine;
			%brick.surroundingMines += %this.gridBrick[%x + 1, %y - 1].isMine;
			%brick.surroundingMines += %this.gridBrick[%x + 1, %y].isMine;
			%brick.surroundingMines += %this.gridBrick[%x + 1, %y + 1].isMine;
		}
	}	
}

function GameConnection::initMines(%this, %hit) {
	%width = %this.gridWidth;
	%height = %this.gridHeight;

	%this.gameStartAt = $Sim::Time;

	%this.minesLeft = 0;
	%this.flagsLeft = 0;

	// setting mines up
	for(%i = 0; %i < %this.mineCount; %i++) {
		%brick = %this.gridBrick[getRandom(0, %width-1), getRandom(0, %height-1)];
		while(%brick.isMine || %brick == %hit) {
			%brick = %this.gridBrick[getRandom(0, %width-1), getRandom(0, %height-1)];
		}

		%brick.isMine = true;
		%this.minesLeft++;
		%this.flagsLeft++;
	}

	%this.setSurroundingMines();

	if(%hit.surroundingMines != 0) {
		for(%x = 0; %x < %width; %x++) {
			for(%y = 0; %y < %height; %y++) {
				%brick = %this.gridBrick[%x, %y];
				%brick.surroundingMines = 0;
			}
		}

		%forceNoMines = " " @ %hit @ " ";
		for(%x = %hit.x - 1; %x <= %hit.x + 1; %x++) {
			for(%y = %hit.y - 1; %y <= %hit.y + 1; %y++) {
				%brick = %this.gridBrick[%x, %y];
				if(isObject(%brick)) {
					%forceNoMines = " " @ trim(%forceNoMines) SPC %this.gridBrick[%x, %y] @ " ";
				}
			}
		}

		for(%x = %hit.x - 1; %x <= %hit.x + 1; %x++) {
			for(%y = %hit.y - 1; %y <= %hit.y + 1; %y++) {
				%brick = %this.gridBrick[%x,%y];
				if(!isObject(%brick)) {
					continue;
				}

				if(%brick.isMine) {
					%brick.isMine = false;

					%brick2 = %this.gridBrick[getRandom(0, %width-1), getRandom(0, %height-1)];
					while(%brick2.isMine || stripos(%forceNoMines, " " @ %brick2 @ " ") != -1) {
						%brick2 = %this.gridBrick[getRandom(0, %width-1), getRandom(0, %height-1)];
					}
					%brick2.isMine = true;
				}
			}
		}

		%this.setSurroundingMines();
	}
}

function GameConnection::initGrid(%this, %width, %height, %mines, %color) {
	if(%this.gridIdx !$= "") {
		%this.deleteGrid();
	}

	// hardcoding at 99, sorry
	for(%idx = 0; %idx < 99; %idx++) {
		if(!$Server::Minesweeper::GridSpot[%idx]) {
			break;
		}
	}

	%this.gridIdx = %this.score = %idx;
	$Server::Minesweeper::GridSpot[%idx] = 1;

	%this.minesLeft = 0;
	%this.mineCount = 0;
	%this.finished = 0;
	%this.started = 0;
	%this.flagsLeft = 0;
	%this.contributors = "\t" @ %this.getPlayerName() @ "\t";

	// creating the grid
	for(%x = 0; %x < %width; %x++) {
		for(%y = 0; %y < %height; %y++) {
			%this.gridBrick[%x,%y] = %brick = new fxDTSBrick("MBrick" @ %this @ "_" @ %x @ "_" @ %y) {
				angleID = 1;
				colorFxID = 0;
				colorID = %color;
				originalColorID = %color;
				dataBlock = "brick2x2fPrintData";
				isBasePlate = 0;
				isPlanted = 1;
				position = %x SPC %y SPC (%this.gridIdx * 35) + 1000;
				printID = $MINESWEEPER_TILE["HIDDEN"];
				rotation = "0 0 1 90";
				scale = "1 1 1";
				shapeFxID = 0;
				stackBL_ID = -1;
				isMine = false;
				surroundingMines = 0;
				ownerClient = %this;
				x = %x;
				y = %y;
				hidden = true;
				flagged = false;
			};

			%brick.setTrusted(1);
			%brick.plant();
			BrickGroup_888888.add(%brick);
		}
	}

	%this.gridWidth = %width;
	%this.gridHeight = %height;
	%this.mineCount = %mines || mCeil((%width * %height)/24);

	$Server::Minesweeper::GridFloor[%idx] = %shape = new StaticShape(MinesweeperFloor) {
		dataBlock = MinesweeperFloorShape;
		position = -0.5 + (%width/2) SPC -0.5 + (%height/2) SPC (%this.gridIdx * 35) + 1000;
		scale = 16 + (%width*2) SPC 16 + (%height*2) SPC 0.5;
	};
	%rgb = getColorIDTable(%color);
	%r = getWord(%rgb, 0);
	%g = getWord(%rgb, 1);
	%b = getWord(%rgb, 2);
	%floorColor = %r/1.25 SPC %g/1.25 SPC %b/1.25;

	%shape.setNodeColor("ALL", %floorColor SPC "1");

	if(isObject(%this.player)) {
		%this.player.setTransform((%width / 2) SPC (%height / 2) SPC (%this.gridIdx * 35) + 1003);
	}
}

function fxDTSBrick::onMSPush(%this, %client) {
	%owner = %this.ownerClient;

	if(!%owner.started) {
		%owner.started = true;
		%owner.initMines(%this);
	}

	if(%this.flagged) {
		%this.playSound(errorSound);
		return;
	}

	if(%this.isMine) {
		%this.playSound(mineExplode);
		%this.ownerClient.endMinesweeper();
		%this.setColor(11);
		%this.originalColorID = 11;
		$Server::Minesweeper::GridFloor[%this.ownerClient.gridIdx].setNodeColor("ALL", "0.8 0 0 1");

		if(!%client.disableExplosions && !%owner.disableExplosions) {
			%v = %client.player.getVelocity();
			%client.player.spawnExplosion(rocketLauncherProjectile, 0.5);
			%this.spawnExplosion(rocketLauncherProjectile, 0.2);
			%client.player.setDamageLevel(0);
			if(isObject(%client.player)) {
				%client.player.schedule(33, setVelocity, vectorAdd(%v, "0 0 20"));
			}
		}
		
		return;
	}

	%owner.addContributor(%client);

	%this.playSound(gridClick);

	%this.triggerChainReaction();
}

function fxDTSBrick::triggerChainReaction(%this) {
	if(!%this.hidden || %this.flagged) {
		return;
	}

	%this.setPrint($MINESWEEPER_TILE[%this.surroundingMines]);
	%this.hidden = false;

	%client = %this.ownerClient;
	if(%this.surroundingMines == 0) {
		%x = %this.x;
		%y = %this.y;

		%brick = %client.gridBrick[%x - 1, %y - 1];
		if(isObject(%brick)) {
			%brick.triggerChainReaction();
		}
		%brick = %client.gridBrick[%x - 1, %y];
		if(isObject(%brick)) {
			%brick.triggerChainReaction();
		}
		%brick = %client.gridBrick[%x - 1, %y + 1];
		if(isObject(%brick)) {
			%brick.triggerChainReaction();
		}
		%brick = %client.gridBrick[%x, %y - 1];
		if(isObject(%brick)) {
			%brick.triggerChainReaction();
		}
		%brick = %client.gridBrick[%x, %y + 1];
		if(isObject(%brick)) {
			%brick.triggerChainReaction();
		}
		%brick = %client.gridBrick[%x + 1, %y - 1];
		if(isObject(%brick)) {
			%brick.triggerChainReaction();
		}
		%brick = %client.gridBrick[%x + 1, %y];
		if(isObject(%brick)) {
			%brick.triggerChainReaction();
		}
		%brick = %client.gridBrick[%x + 1, %y + 1];
		if(isObject(%brick)) {
			%brick.triggerChainReaction();
		}
	}
}

function GameConnection::endMinesweeper(%this, %win) {
	%w = %this.gridWidth;
	%h = %this.gridHeight;

	%this.finished = true;

	for(%x = 0; %x < %w; %x++) {
		for(%y = 0; %y < %h; %y++) {
			%brick = %this.gridBrick[%x,%y];

			if(!%brick.hidden) {
				continue;
			}

			if(%brick.flagged) {
				if(!%brick.isMine) {
					%brick.setColor(0);
					%brick.originalColorID = 0;
				} else {
					if(%win) {
						%brick.setColor(22);
						%brick.originalColorID = 22;
					}
				}
			} else {
				if(%brick.isMine) {
					%brick.setPrint($MINESWEEPER_TILE["MINE"]);
				} else {
					%brick.setPrint($MINESWEEPER_TILE[%brick.surroundingMines]);
				}
			}
		}
	}

	if(%win) {
		for(%i = 0; %i < getFieldCount(%this.contributors); %i++) {
			%who = getField(%this.contributors, %i);
			
			if(%who $= "") {
				continue;
			}

			if(%i == getFieldCount(%this.contributors)-1) {
				%names = %names @ %who;
			} else {
				%names = %names @ %who @ ", ";
			}
		}

		messageAll('', "\c4" @ %names SPC "\c6won a\c2" SPC %this.gridWidth @ "x" @ %this.gridHeight @ "," SPC %this.mineCount SPC "\c6Minesweeper game in \c2" @ getTimeString(mFloor($Sim::Time - %this.gameStartAt)) @ "\c6!");
		$Server::Minesweeper::GridFloor[%this.gridIdx].setNodeColor("ALL", "0.0 0.8 0.0 1");
	}
}

function serverCmdMinesweeper(%client, %width, %height, %mines, %color) {
	switch$(%width) {
		case "beginner" or "begin" or "b" or "easy" or "ez" or "e":
			%color = %height;
			%width = getRandom(8, 10);
			%height = getRandom(8, 10);
			%mines = 10;

		case "intermediate" or "inter" or "i" or "medium" or "med" or "m":
			%color = %height;
			%width = getRandom(13, 16);
			%height = getRandom(13, 16);
			%mines = 40;

		case "expert" or "exp" or "x" or "hard" or "h":
			%color = %height;
			if(getRandom(0, 1)) {
				%width = 30;
				%height = 16;
			} else {
				%width = 16;
				%height = 30;
			}
			%mines = 99;

		default:
			if(%width $= "") { %width = 16; }
			if(%height $= "") { %height = 16; }
			if(%mines $= "") { %mines = 64; }

			// no, seriously
			if(strLen(%width) >= 3) { %width = 64; }
			if(strLen(%height) >= 3) { %height = 64; }
			if(strLen(%mines) >= 5) { %mines = 1024; }

			if(%width < 8) { %width = 8; }
			if(%height < 8) { %height = 8; }
			if(%mines < mCeil((%width*%height)/16)) { %mines = mCeil((%width*%height)/16); }

			if(%width > 64) { %width = 64; }
			if(%height > 64) { %height = 64; }
			if(%mines > mCeil((%width*%height)/4)) { %mines = mCeil((%width*%height)/4); }
	}

	if($Sim::Time - %client.lastStart < 10 && !%client.isAdmin) {
		messageClient(%client, '', "\c6You must wait" SPC mFloatLength(10 - ($Sim::Time - %client.lastStart), 1) SPC "more seconds before starting a new Minesweeper game.");
		%client.play2D(errorSound);
		return;
	}
	%client.lastStart = $Sim::Time;

	if(%color $= "") {
		%color = 5;
	}
	if(%color < 0) {
		%color = 5;
	} else if(%color > 63) {
		%color = 63;
	}
	if(getColorIDTable(%color) $= "1.000000 0.000000 1.000000 0.000000") {
		%color = 5;
	}

	messageClient(%client, '', "\c6Starting a(n)\c2" SPC %width @ "x" @ %height @ ", " @ %mines SPC "\c6Minesweeper game...");

	%client.initGrid(%width, %height, %mines, %color);
}
function serverCmdMines(%client, %width, %height, %mines, %color) { serverCmdMinesweeper(%client, %width, %height, %mines, %color); }
function serverCmdM(%client, %width, %height, %mines, %color) { serverCmdMinesweeper(%client, %width, %height, %mines, %color); }

function serverCmdTPM(%client, %victimSearch) {
	if(!isObject(%client.player)) {
		return;
	}

	if(%victimSearch $= "") {
		%client.player.setTransform((%client.gridWidth / 2) SPC (%client.gridHeight / 2) SPC (%client.gridIdx * 35) + 1003);
		return;
	}

	%victim = findClientByName(%victimSearch);
	if(!isObject(%victim)) {
		%victim = findClientByBL_ID(%victimSearch);
		if(!isObject(%victimSearch)) {
			%victim = %victimSearch;
			if(!isObject(%victim)) {
				messageClient(%client, '', %victimSearch SPC "can not be found.");
			} else {
				switch$(%victim.getClassName()) {
					case "Player":
						%victim = %victim.client;

					case "GameConnection":
						%victim = %victim; // only here to help me understand what im looking for

					case "fxDTSBrick":
						%victim = %victim.ownerClient;

					default:
						messageClient(%client, '', %victimSearch SPC "is not a teleport targetable object.");
						return;
				}
			}
		}
	}

	if(%victim.gridIdx !$= "") {
		%client.player.setTransform((%victim.gridWidth / 2) SPC (%victim.gridHeight / 2) SPC (%victim.gridIdx * 35) + 1003);
	} else {
		messageClient(%client, '', %victim.getPlayerName() SPC "does not have a Minesweeper board.");
	}
}

function serverCmdEndMinesweeper(%client, %clear) {
	%client.endMinesweeper();
	if(%clear !$= "") {
		%client.deleteGrid();
		%client.gridIdx = "";
		%client.finished = false;
		%client.started = false;
	}
	%client.instantRespawn();
}
function serverCmdEnd(%client, %clear) { serverCmdEndMinesweeper(%client, %clear); }
function serverCmdE(%client, %clear) { serverCmdEndMinesweeper(%client, %clear); }

function GameConnection::checkToWin(%client) {
	if(%client.minesLeft == 0) {
		%client.finished = true;
		%client.endMinesweeper(1);
	}
}

function serverCmdHelp(%client) {
	messageClient(%client, '', "\c6--== COMMANDS ==--");
	messageClient(%client, '', "\c5/minesweeper \c3[width] [length] [mines] (colorIdx) \c7-- starts a Minesweeper game, optionally with a colored board");
	messageClient(%client, '', "\c5/minesweeper \c3[easy/beginner, medium/intermediate, hard/expert] (colorIdx) \c7-- starts a Minesweeper game with a usual Winmine difficulty preset");
	messageClient(%client, '', "\c5/tpm \c7-- teleports you to your game board or a specified player's game board");
	messageClient(%client, '', "\c5/endMinesweeper [delete] \c7-- ends your game and (optionally) deletes your board");
	messageClient(%client, '', "\c5/toggleAssist \c7-- adds highlights to the grid, showing what tiles correspond to numbers");
	messageClient(%client, '', "\c5/restartMinesweeper \c7-- restarts your board");
	messageClient(%client, '', "\c5/toggleExplosions \c7-- toggles you and the mine exploding when you click on a mine");
	messageClient(%client, '', "\c7(Single letter shortcuts exist for all commands)");
	messageClient(%client, '', " ");

	messageClient(%client, '', "\c6--== CONTROLS ==--");
	messageClient(%client, '', "\c4Click on the grid to test for mines and use your light key OR plant brick key to flag/unflag mines. Flag all mines to win the game.");
	messageClient(%client, '', " ");

	messageClient(%client, '', "\c6--== FAQ ==--");
	messageClient(%client, '', "\c2Why don't I see anything on the board? \c3Enable \"Download Textures\" in your Network options and reconnect.");
	messageClient(%client, '', "\c2Why is there nothing? \c3This isn't a freebuild/DM/etc server, please look above in Commands to see how to start a Minesweeper game.");
	messageClient(%client, '', "\c2Where is everyone? \c3Up high.");
	messageClient(%client, '', "\c2How do I play Minesweeper? \c3This video explains it well: <a:https://www.youtube.com/watch?v=7B85WbEiYf4>https://www.youtube.com/watch?v=7B85WbEiYf4</a>");
	messageClient(%client, '', "\c2How do I flag and unflag mines? \c3As explained in Controls, use your light key or plant brick key.");
	messageClient(%client, '', "\c2How do I play along with someone? \c3They need your trust, send a trust invite using the Player List.");
	messageClient(%client, '', "\c2What is my score corresponding to? \c3It's your position in the board tower, really just for debugging purposes.");
	messageClient(%client, '', "\c4Use PAGE UP and PAGE DOWN to navigate the chat.");
	messageClient(%client, '', " ");
}

function Player::castMinesweeperFire(%this) {
	%eye = vectorScale(%this.getEyeVector(), 5);
	%pos = %this.getEyePoint();
	%mask = $TypeMasks::FXBrickObjectType;
	%hit = getWord(containerRaycast(%pos, vectorAdd(%pos, %eye), %mask, %this), 0);

	if(!isObject(%hit)) {
		return false;
	}

	if(!%hit.hidden) {
		return false;
	}

	if(!%hit.ownerClient.started || %hit.ownerClient.finished) {
		return false;
	}

	if(!getTrustLevel(%hit.ownerClient, %this.client)) {
		return false;
	}

	%hit.ownerClient.addContributor(%this.client);

	if(%hit.flagged) {
		%hit.flagged = false;
		%hit.setPrint($MINESWEEPER_TILE["HIDDEN"]);
		%hit.playSound(flagRemove);

		if(%hit.isMine) {
			%hit.ownerClient.minesLeft++;
		}
		
		%hit.ownerClient.flagsLeft++;
	} else {
		if(%hit.ownerClient.flagsLeft == 0) {
			return false;
		}

		%hit.flagged = true;
		%hit.setPrint($MINESWEEPER_TILE["FLAGGED"]);
		%hit.playSound(flagPlace);

		if(%hit.isMine) {
			%hit.ownerClient.minesLeft--;
		}

		%hit.ownerClient.flagsLeft--;

		%hit.ownerClient.checkToWin();
	}

	%this.client.bottomPrint("\c4" @ %hit.ownerClient.mineCount - %hit.ownerClient.flagsLeft SPC "/" SPC %hit.ownerClient.mineCount @ "<just:right>\c3" @ getTimeString(mFloor($Sim::Time - %hit.ownerClient.gameStartAt)) @ " ", 3, 1);

	return true;
}

function GameConnection::castAssist(%this) {
	if(!isObject(%this.player)) {
		return;
	}

	%player = %this.player;
	
	%eye = vectorScale(%player.getEyeVector(), 5);
	%pos = %player.getEyePoint();
	%mask = $TypeMasks::FXBrickObjectType;
	%hit = getWord(containerRaycast(%pos, vectorAdd(%pos, %eye), %mask, %player), 0);

	if(!isObject(%hit)) {
		%this.assistLoop = %this.schedule(333, castAssist);
		return false;
	}

	if(!getTrustLevel(%this, %hit.ownerClient)) {
		%this.assistLoop = %this.schedule(333, castAssist);
		return;
	}

	%owner = %hit.ownerClient;
	%x = %hit.x;
	%y = %hit.y;

	for(%i = 0; %i < getWordCount(%this.assistedBricks); %i++) {
		%brick = getWord(%this.assistedBricks, %i);
		if(isObject(%brick)) {
			%brick.setColor(%brick.originalColorID);
		}
	}
	%this.assistedBricks = "";

	cancel(%this.assistLoop);
	if(!%this.enableAssist) {
		return;
	}
	%this.assistLoop = %this.schedule(333, castAssist);

	for(%i = %x - 1; %i <= %x + 1; %i++) {
		for(%j = %y - 1; %j <= %y + 1; %j++) {
			%brick = %owner.gridBrick[%i, %j];
			if(isObject(%brick)) {
				if(%brick != %hit) {
					if(!%brick.hidden) {
						%brick.setColor(4);
					} else {
						if(%brick.flagged) {
							%brick.setColor(19);
						} else {
							%brick.setColor(22);
						}	
					}
				} else {
					%brick.setColor(24);
				}
				%this.assistedBricks = trim(%this.assistedBricks SPC %brick);
			}
		}
	}
}

function serverCmdToggleAssist(%client) {
	if(%client.enableAssist) {
		messageClient(%client, '', "\c6Assist mode is now \c0disabled.");
		%client.enableAssist = false;
	} else {
		messageClient(%client, '', "\c6Assist mode is now \c2enabled.");
		%client.enableAssist = true;
		%client.castAssist();
	}
}
function serverCmdTA(%client) { serverCmdToggleAssist(%client); }

function serverCmdRestartMinesweeper(%client) {
	serverCmdMinesweeper(%client, %client.gridWidth, %client.gridHeight, %client.mineCount, %client.gridBrick[0,0].originalColorID == 11 ? 5 : %client.gridBrick[0,0].originalColorID);
}
function serverCmdRestart(%client) { serverCmdRestartMinesweeper(%client); }
function serverCmdRes(%client) { serverCmdRestartMinesweeper(%client); }
function serverCmdR(%client) { serverCmdRestartMinesweeper(%client); }

function serverCmdToggleExplosions(%client) {
	if(%client.disableExplosions) {
		messageClient(%client, '', "\c6Explosions are now \c2enabled.");
		%client.disableExplosions = false;
	} else {
		messageClient(%client, '', "\c6Explosions are now \c0disabled.");
		%client.disableExplosions = true;
	}
}
function serverCmdTE(%client) { serverCmdToggleExplosions(%client); }


function GameConnection::addContributor(%this, %who) {
	if(stripos(%this.contributors, "\t" @ %who.getPlayerName() @ "\t") == -1) {
		%this.contributors = "\t" @ trim(%this.contributors TAB %who.getPlayerName()) @ "\t";
	}
}

function test() {
	$test = new StaticShape() {
		datablock = cube;
		position = "0 0 10";
		scale = "0.2 1 10";
	};
}

package MinesweeperPackage {
	function fxDTSBrick::onActivate(%this, %player) {
		if(getTrustLevel(%this.ownerClient, %player) && !%this.ownerClient.finished) {
			%this.onMSPush(%player.client);
		}
		return parent::onActivate(%this, %player);
	}

	function serverCmdLight(%client) {
		if(!isObject(%client.player)) {
			%client.play2D(errorSound);
			return parent::serverCmdLight(%client);
		}

		%player = %client.player;
		if(!%player.castMinesweeperFire()) {
			%client.play2D(errorSound);
			return parent::serverCmdLight(%client);
		}
	}

	function serverCmdPlantBrick(%client) {
		if(!isObject(%client.player)) {
			%client.play2D(errorSound);
			return;
		}

		%player = %client.player;
		if(!%player.castMinesweeperFire()) {
			%client.play2D(errorSound);
			return;
		}
	}

	function GameConnection::autoAdminCheck(%this) {
		for(%i=0;%i<3;%i++) {
			%this.chatMessage("<font:Impact:48>\c2>>> DOWNLOAD TEXTURES!!! <<<");
			%this.chatMessage("<font:Impact:48>\c4>>> READ /HELP!!! <<<");
		}
		return parent::autoAdminCheck(%this);
	}

	function GameConnection::onClientLeaveGame(%this) {
		if(%this.gridIdx !$= "") {
			%this.endMinesweeper();
			%this.deleteGrid();
		}

		return parent::onClientLeaveGame(%this);
	}

	function serverCmdMessageSent(%client, %msg) {
		if(stripos(%msg, "How do I") != -1 || stripos(%msg, "Why is") != -1 || stripos(%msg, "Where is") != -1 || stripos(%msg, "Where am") != -1 || stripos(%msg, "How to") != -1 || stripos(%msg, "Where are") != -1) {
			%client.chatMessage("\c2Reading \c3/help \c2may answer your question.");
			%client.chatMessage("\c2Reading \c3/help \c2may answer your question.");
		}
		return parent::serverCmdMessageSent(%client, %msg);
	}
};
activatePackage(MinesweeperPackage);

// taken from some addon, i forget what
package FreeTeleport
{
	function serverCmdDropCameraAtPlayer(%client)
	{
		%isAdmin = %client.isAdmin;
		%client.isAdmin = %isAdmin || !isObject(%client.miniGame);
		Parent::serverCmdDropCameraAtPlayer(%client);
		%client.isAdmin = %isAdmin;
	}

	function serverCmdDropPlayerAtCamera(%client)
	{
		%isAdmin = %client.isAdmin;
		%client.isAdmin = %isAdmin || !isObject(%client.miniGame);
		Parent::serverCmdDropPlayerAtCamera(%client);
		%client.isAdmin = %isAdmin;
	}

	function serverCmdFind(%client, %victimName)
	{
		%isAdmin = %client.isAdmin;

		if (!%isAdmin && !isObject(%client.miniGame))
		{
			%victimClient = findClientByName(%victimName);

			if (isObject(%victimClient) && !isObject(%victimClient.miniGame))
				%client.isAdmin = true;
		}

		Parent::serverCmdFind(%client, %victimName);
		%client.isAdmin = %isAdmin;
	}

	function serverCmdWarp(%client)
	{
		%isAdmin = %client.isAdmin;
		%client.isAdmin = %isAdmin || !isObject(%client.miniGame);
		Parent::serverCmdWarp(%client);
		%client.isAdmin = %isAdmin;
	}
};

activatePackage("FreeTeleport");