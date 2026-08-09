from dataclasses import dataclass

from Options import PerGameCommonOptions, Toggle, Choice, Range

class ProgressionMode(Choice):
	"""
	How should the game progress?

	Vanilla: You go through cars as you do in the vanilla game
	Entrance Rando: The exits to each car is randomized
	Car Rando: Puts the train cars into the item pool
	"""
	display_name = "Progression Mode"

	option_vanilla = 0
	option_entrance_rando = 1
	option_car_rando = 2

class DialogSanity(Choice):
	"""
	Should the 76 choices in dialogs be checks
	Optionally including the 64 missable choices
	- Will require you to load a save or re-enter the car
	"""
	display_name = "Choice Sanity"

	option_off = 0
	option_on = 1
	option_include_missable = 2

	default = option_off

class DeductionSanity(Choice):
	"""
	Should the 31 correct deductions be checks. 
	Optionally including the 54 incorrect deductions.
	- Note that this does not override deathlink if enabled
	"""
	display_name = "Deduction Sanity"
	
	option_off = 0
	option_on = 1
	option_include_incorrect = 2

	default = option_off

class RunnerSanity(Toggle):
	"""
	Should the 24 running minigames be checks?
	"""
	display_name = "Runner Sanity"

class CarDupe(Range):
	"""
	How many duplicates of the car items should be in the pool?
	"""
	display_name = "Car Duplicates"

	range_start = 1
	range_end = 50

	default = 1

class InvDupe(Range):
	"""
	How many duplicates of the inventory items should be in the pool?
	"""
	display_name = "Inventory Duplicates"

	range_start = 1
	range_end = 50

	default = 1

@dataclass
class TMOSTHOptions(PerGameCommonOptions):
	progression_mode: ProgressionMode
	car_dupe: CarDupe
	inv_dupe: InvDupe
	choice_sanity: DialogSanity
	deduction_sanity: DeductionSanity
	runner_sanity: RunnerSanity

