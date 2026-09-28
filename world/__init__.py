from worlds.AutoWorld import World
from Options import OptionError

from .items import ItemState
from .locations import LocationState
from .options import TMOSTHOptions

from .apshared import apshared
from .apshared import location_name_to_id as lname_to_id
from .apshared import item_name_to_id as iname_to_id

class TMOSTHWorld(World):
	ut_can_gen_without_yaml = True
	
	game = apshared["game"]
	location_name_to_id = lname_to_id
	item_name_to_id = iname_to_id

	options_dataclass = TMOSTHOptions
	options: TMOSTHOptions

	item_state = ItemState()
	
	def generate_early(self):
		self.location_state = LocationState()
		self.item_state = ItemState()

		if self.options.progression_mode == 2:
			self.car_rando = True
			self.entrance_rando = False
		else:
			self.car_rando = False
			self.entrance_rando = self.options.progression_mode == 1

		if self.options.runner_sanity:
			self.location_state.sanities.append("run")
		if self.options.choice_sanity:
			self.location_state.sanities.append("dialog")
		if self.options.choice_sanity.value == 2:
			self.location_state.sanities.append("missable")
		if self.options.deduction_sanity:
			self.location_state.sanities.append("deduct")
		if self.options.deduction_sanity.value == 2:
			self.location_state.sanities.append("incorrect")
		
		self.inv_dupe = self.options.inv_dupe.value
		self.car_dupe = self.options.car_dupe.value
		
		self.location_state.regen = False
		re_gen_passthrough = getattr(self.multiworld, "re_gen_passthrough", {})
		if re_gen_passthrough and self.game in re_gen_passthrough:
			slot_data = re_gen_passthrough[self.game]
			self.car_rando = slot_data["car_rando"]
			self.location_state.sanities = slot_data["sanities"]
			self.location_state.er_regions = slot_data["entrances"]
			self.location_state.regen = True
			return

		location_count = 84
		item_count = self.inv_dupe * 14
		if self.car_rando:
			item_count += self.car_dupe * 7

		if self.options.runner_sanity: location_count += 24
		if self.options.choice_sanity: location_count += 76
		if self.options.choice_sanity == 2: location_count += 64
		if self.options.deduction_sanity: location_count += 31
		if self.options.deduction_sanity == 2: location_count += 54

		if item_count > location_count: raise OptionError("Inventory Dupe or Car Dupe is too high!")

	def create_regions(self):
		self.location_state.setup_regions(self)

	def create_items(self):
		self.item_state.setup_items(self)

	def create_item(self, name):
		return self.item_state.create_item(self, name)

	def get_filler_item_name(self):
		return self.item_state.get_filler_item(self)

	def write_spoiler(self, spoiler_handle):
		spoiler_text = ""
		spoiler_text += f"\nCar Order for {self.multiworld.player_name[self.player]}:\n"
		for car in self.location_state.er_regions:
			spoiler_text += f"\t{car}\n"
		spoiler_text += "\n"
		spoiler_handle.write(spoiler_text)

	def fill_slot_data(self):
		return {
			"version": apshared["version"],
			"car_rando": self.car_rando,
			"sanities": self.location_state.sanities,
			"entrances": self.location_state.er_regions
		}

	@staticmethod
	def interpret_slot_data(slot_data):
		return slot_data