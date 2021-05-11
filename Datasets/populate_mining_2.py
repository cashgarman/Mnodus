import csv
import json
from random import shuffle, choice, randint, uniform
from bson.objectid import ObjectId

# Objects
cityPath = "Mining Data 2/CITY_TABLE.csv"
companyPath = "Mining Data 2/COMPANY_TABLE.csv"
industryPath = "Mining Data 2/INDUSTRY_TABLE.csv"
peoplePath = "Mining Data 2/PERSON_TABLE.csv"

# Links
companyCityPath = "Mining Data 2/COMPANY_CITY_TABLE.csv"
companyIndustryPath = "Mining Data 2/COMPANY_INDUSTRY_TABLE.csv"
companyPersonPath = "Mining Data 2/COMPANY_PERSON_TABLE.csv"

# Inital Values
companySharesOutPath = "Mining Data 2/COMPANY_SHARES_OUT_TABLE.csv"
companyStockOpenPath = "Mining Data 2/COMPANY_STOCK_OPEN_TABLE.csv"

# Temporal (HACKED DATA)
dayTradingPath = "Mining Data 2/DAY_OF_TRADING_%d.csv"
numDays = 6

# Output
outputPath = "../Assets/Resources/DatasetsJSON/mining_2.json"

class Dataset(object):
	def __init__(self):
		self.name = ""
		self.nodes = []
		# self.links = []
		# self.metadata = {}

	# Credit: http://stackoverflow.com/questions/3768895/python-how-to-make-a-class-json-serializable
	def to_JSON(self):
		return json.dumps(self, default=lambda o: o.__dict__, sort_keys=True, indent=4)

globalID = 0
class DatasetNode(object):
	def __init__(self):
		global globalID
		self.name = ""
		self.type = ""
		self.ID = globalID
		self.metadata = {}
		globalID += 1

	def __repr__(self):
		print(self.name)
		print(self.type)
		print(self.ID)
		print(repr(self.metadata))

	def set(self, name, value):
		setattr(self, name, value)

class DatasetLink(object):
	def __init__(self):
		self.sourceType = ""
		self.targetType = ""
		self.source = 0
		self.target = 0
		self.value = 0

def CreateLink(a, b, dataset):
	link = DatasetLink()
	link.sourceType = ""
	link.targetType = ""
	link.source = FindNodeIndexByName(dataset, a)
	link.target = FindNodeIndexByName(dataset, b)
	return link

def ParseCSV(path):
	output = []
	with open(path) as csvfile:
		reader = csv.DictReader(csvfile)
		for row in reader:
			output.append(row)
	return output

## Objects ##

cities = ParseCSV(cityPath)
print("Found %d cities" % len(cities))

industries = ParseCSV(industryPath)
print("Found %d industries" % len(industries))

companies = ParseCSV(companyPath)
print("Found %d companies" % len(companies))

people = ParseCSV(peoplePath)
print("Found %d people" % len(people))

## Relationships ##

companyCityLinks = ParseCSV(companyCityPath)
print("Found %d company city relationships" % len(companyCityLinks))
# shuffle(companyCityLinks)

companyIndustryLinks = ParseCSV(companyIndustryPath)
print("Found %d company industry relationships" % len(companyIndustryLinks))

companyPersonLinks = ParseCSV(companyPersonPath)
print("Found %d project company person relationships" % len(companyPersonLinks))

## Metadata ##

companySharesOut = ParseCSV(companySharesOutPath)
print("Found %d company share entries" % len(companySharesOut))

companyShareOpeningValues = ParseCSV(companyStockOpenPath)
print("Found %d company share opening values" % len(companyShareOpeningValues))

# Load the day trading numbers
dayTradings = []
for i in range(numDays):
	content = ParseCSV(dayTradingPath % (i + 1))
	dayTradings.append(content)

dayTradings += reversed(dayTradings)

def FindObjectIndex(ID, objects, IDName):
	for i, obj in enumerate(objects):
		if obj[IDName] == ID:
			return i
	return -1

def AddNode(node):
	alreadyExists = False
	for n in dataset.nodes:
		if n.name is node.name and n.type is node.type:
			alreadyExists = True
	if not alreadyExists:
		dataset.nodes.append(node)

	return node

def CreateNode(ID, name, type, metadata = {}):
	node = DatasetNode()
	node._id = str(ObjectId())
	node.originalID = int(ID)
	node.name = name
	node.type = type
	node.metadata = metadata
	return node

def CreateLink(soureType, sourceIndex, targetType, targetIndex):
	link = DatasetLink()
	link.sourceType = soureType
	link.source = sourceIndex
	link.targetType = targetType
	link.target = targetIndex
	return link

def AddObjects(objects, type, nameName, IDName):
	addedNodes = []
	for obj in objects:
		index = FindObjectIndex(obj[IDName], objects, IDName)
		addedNodes.append(AddNode(CreateNode(index, obj[nameName], type, obj)))
	print("Added %d of %s" % (len(addedNodes), type))
	return addedNodes

def AddLinks(links, sourceObjects, sourceType, targetObjects, targetType, sourceIDName, targetIDName):
	for link in links:
		source = FindNodeByOriginalID(link[sourceIDName], sourceObjects, sourceIDName);
		target = FindNodeByOriginalID(link[targetIDName], targetObjects, targetIDName);
		
		if source is None:
			# print "Couldn't find source %s with ID %s" % (sourceType, link[sourceIDName])
			continue
		elif target is None:
			# print "Couldn't find target %s with ID %s" % (targetType, link[targetIDName])
			continue
		# else:
		# 	dataset.links.append(CreateLink(sourceType, source.originalID, targetType, target.originalID))

		# Add the links the source and target nodes
		sourceLinks = getattr(source, targetIDName, [])
		sourceLinks.append(target._id)
		source.set(targetIDName, sourceLinks)

		targetLinks = getattr(target, sourceIDName, [])
		targetLinks.append(source._id)
		target.set(sourceIDName, targetLinks)

		# print "Linked %s to %s" % (source.name, target.name)

def FindNodeByOriginalID(ID, objects, IDName):
	for obj in objects:
		if isinstance(obj, DatasetNode):
			if int(obj.originalID) == (int(ID) - 1):
				return obj
	return None

def AttachMetadata(entries, objects, objectIDName, metadataIDName, metadataName):
	for entry in entries:
		target = FindNodeByOriginalID(entry[objectIDName], objects, objectIDName)
		target.set(metadataName, entry[metadataIDName])

dataset = Dataset()
dataset.name = "Mining Industry"

# Add all the objects
cityNodes = AddObjects(cities, "City", "city_name", "city_id")
companyNodes = AddObjects(companies, "Company", "company_name", "company_id")
industryNodes = AddObjects(industries, "Industry", "industry_name", "industry_id")
personNodes = AddObjects(people, "Person", "name", "person_id")

# Add the initial stock data
AttachMetadata(companySharesOut, companyNodes, "company_id", "company_shares", "initial_shares")
# AttachMetadata(companyShareOpeningValues, companyNodes, "company_id", "company_stock_open", "share_value")

# Assign random trading data to all companies
for companyNode in companyNodes:
	companyNode.metadata["dayTradingIndices"] = []
	for day in range(numDays):
		companyNode.metadata["dayTradingIndices"].append(randint(0, numDays * 2 - 1))
	# companyNode.set("share_value", uniform(0.25, 0.75))

# Add all the relationships
AddLinks(companyCityLinks, companyNodes, "Company", cityNodes, "City", "company_id", "city_id")
AddLinks(companyIndustryLinks, companyNodes, "Company", industryNodes, "Industry", "company_id", "industry_id")
AddLinks(companyPersonLinks, companyNodes, "Company", personNodes, "Person", "company_id", "person_id")

# Add the dataset metadata
metadata = CreateNode(0, "Metadata", "Metadata")
metadata.set("dayTradingNumbers", dayTradings)
#metadata.set("minStockAmount", min(companySharesOut))
#metadata.set("maxStockAmount", max(companySharesOut))
dataset.nodes.append(metadata)

open(outputPath, "w").write(dataset.to_JSON())
print("Wrote output JSON to %s" % outputPath)