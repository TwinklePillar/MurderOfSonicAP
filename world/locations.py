from BaseClasses import Region, Location
from rule_builder.rules import True_, Has
from .apshared import apshared, location_name_to_id
from .items import TMOSTHItem

class TMOSTHLocation(Location):
	game = apshared["game"]

class LocationState:
	def __init__(self):
		self.er_regions = []
		self.sanities = ["obj", "scr"]
		self.regen = False

	def setup_regions(self, world):
		all_regions = []
		menu_region = Region("Menu", world.player, world.multiworld)
		world.multiworld.regions.append(menu_region)
		for room in apshared["rooms"]:
			region = Region(room["name"], world.player, world.multiworld)
			world.multiworld.regions += [region]
			all_regions.append(region)
			rule = True_()
			for check in room["checks"]:
				check_name = f"{room['name']} {check['name']}"
				if check_name == "Final Push Runner #11":
					region.add_event("Flicky Saved", "Retirement", location_type=TMOSTHLocation, item_type=TMOSTHItem)
				else:
					if check["sanity"] in self.sanities:
						region.add_locations({check_name: location_name_to_id[check_name]}, TMOSTHLocation)

		if not self.regen:
			self.er_regions = ["Prologue", "DiningCloset", "Saloon", "Library", "Casino", "Lounge", "Conductor_Car", "LockdownDiningCar"]
			if world.entrance_rando:
				world.random.shuffle(self.er_regions)
			self.er_regions = ["Name_Entry"] + self.er_regions + ["Final_Push"]

		env_to_regions = {
			"Name_Entry": ["Menu", "Menu", False],
			"Prologue": ["Dining Car", "Dining Car Interlude", True],
			"DiningCloset": ["Dining Closet", "Dining Closet Interrogation", True],
			"Saloon": ["Saloon", "Saloon Interrogation", True],
			"Library": ["Library", "Library Interrogation", True],
			"Casino": ["Casino", "Casino", True],
			"Lounge": ["Lounge", "Lounge Interrogation", True],
			"Conductor_Car": ["Conductor's Car", "Conductor's Car Interrogation", True],
			"LockdownDiningCar": ["Lockdown", "Lockdown", False],
			"Final_Push": ["Final Push", "Final Push", False]
		}
		connections = []
		for connection in apshared["connections"]:
			if "fixed" in connection:
				connections.append(connection)
		for i in range(len(self.er_regions)-1):
			connection = {"from": env_to_regions[self.er_regions[i]][1], "to": env_to_regions[self.er_regions[i+1]][0], "rule": []}
			if env_to_regions[self.er_regions[i+1]][2]:
				connection["car_rando"] = 1
			connections.append(connection)

		for connection in connections:
			from_name = connection["from"]

			if "car_rando" in connection and world.car_rando:
				from_name = "Menu"
			from_region = world.get_region(from_name)
			to_region = world.get_region(connection["to"])
			rule = True_()
			for item in connection["rule"]:
				rule = rule & Has(item)
			print(f"{from_region.name} -> {to_region.name} ({rule})")
			from_region.connect(to_region, f"{from_region.name} to {to_region.name}")
			enter = world.get_entrance(f"{from_region.name} to {to_region.name}")
			world.set_rule(enter, rule)
		world.set_completion_rule(Has("Retirement"))