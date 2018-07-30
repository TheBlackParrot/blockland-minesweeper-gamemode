datablock AudioProfile(gridClick)
{
	filename = "./sounds/gridClick.wav";
	description = AudioClosest3d;
	preload = true;
};
datablock AudioProfile(flagPlace:gridClick) { filename = "./sounds/flagPlace.wav"; };
datablock AudioProfile(flagRemove:gridClick) { filename = "./sounds/flagRemove.wav"; };
datablock AudioProfile(mineExplode:gridClick) { filename = "./sounds/mineExplode.wav"; };