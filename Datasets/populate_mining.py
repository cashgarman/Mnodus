import csv
import json
from random import shuffle

# Objects
personPath = "Mining Data/Person.csv"
companyPath = "Mining Data/Company.csv"
commodityPath = "Mining Data/Commodity.csv"
projectPath = "Mining Data/Project.csv"
stagePath = "Mining Data/Stage.csv"

# Links
personCompanyPath = "Mining Data/PersonCompany.csv"
projectCompanyPath = "Mining Data/ProjectCompany.csv"
projectCommodityPath = "Mining Data/ProjectCommodity.csv"

outputPath = "../ForceDirectedGraphs/Datasets/mining.json"

class Dataset(object):
	def __init__(self):
		self.name = ""
		self.nodes = []
		self.links = []

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

people = ParseCSV(personPath)
people = people[0:1000]
print "Found %d people" % len(people)

companies = ParseCSV(companyPath)
print "Found %d companies" % len(companies)

commodities = ParseCSV(commodityPath)
print "Found %d commodities" % len(commodities)

projects = ParseCSV(projectPath)
print "Found %d projects" % len(projects)

stages = ParseCSV(stagePath)
print "Found %d project stages" % len(stages)

personCompanyLinks = ParseCSV(personCompanyPath)
print "Found %d person company relationships" % len(personCompanyLinks)
shuffle(personCompanyLinks)

projectCompanyLinks = ParseCSV(projectCompanyPath)
print "Found %d project company relationships" % len(projectCompanyLinks)

projectCommodityLinks = ParseCSV(projectCommodityPath)
print "Found %d project commodity relationships" % len(projectCommodityLinks)

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

def CreateNode(ID, name, type, metadata):
	node = DatasetNode()
	node.ID = ID
	node.name = name
	node.type = type
	# node.metadata = metadata
	return node

def CreateLink(soureType, sourceIndex, targetType, targetIndex):
	link = DatasetLink()
	link.sourceType = soureType
	link.source = sourceIndex
	link.targetType = targetType
	link.target = targetIndex
	return link

def AddObjects(objects, type, IDName):
	for obj in objects:
		index = FindObjectIndex(obj[IDName], objects, IDName)
		AddNode(CreateNode(index, obj["Name"], type, obj))

def AddLinks(links, sourceObjects, sourceType, targetObjects, targetType, number = -1):
	linkSet = []
	if number is -1:
		linkSet = links
	else:
		linkSet = links[0:number]
		print "Using %d links" % len(linkSet)

	for link in linkSet:
		sourceIndex = FindObjectIndex(link[sourceType + "ID"], sourceObjects, sourceType + "ID")
		targetIndex = FindObjectIndex(link[targetType + "ID"], targetObjects, targetType + "ID")
		if sourceIndex is -1:
			print "Couldn't find source %s with ID %s" % (sourceType, link[sourceType + "ID"])
		elif targetIndex is -1:
			print "Couldn't find target %s with ID %s" % (targetType, link[targetType + "ID"])
		else:
			dataset.links.append(CreateLink(sourceType, sourceIndex, targetType, targetIndex))

def RemoveUnlinkedNodes():
	numNodes = len(dataset.nodes)
	for node in list(dataset.nodes):
		linked = False

		for link in dataset.links:
			if node.ID is link.source or node.ID is link.target:
				linked = True
				break

		# if node.type is "Company":
		# 	for link in dataset.links:

		# 		if link.sourceType is "Company" and node.ID is link.source:
		# 			print "Found source link to %s" % node.name
		# 			linked = True

		# 		if link.targetType is "Company" and node.ID is link.target:
		# 			print "Found target link to %s" % node.name
		# 			linked = True

		# elif node.type is "Person":
		# 	for link in dataset.links:

		# 		if link.sourceType is "Person" and node.ID is link.source:
		# 			print "Found source link to %s" % node.name
		# 			linked = True

		# 		if link.targetType is "Person" and node.ID is link.target:
		# 			print "Found target link to %s" % node.name
		# 			linked = True

		# elif node.type is not "Company" and node.type is not "Person":
		# 	linked = True

		# If the node isn't referenced anywhere
		if not linked:
			# print "Removing unused %s %s with ID %d" % (node.type, node.name, node.ID)
			# Remove it from the dataset
			dataset.nodes.remove(node)

	print "Removed %d unlinked nodes" % (numNodes - len(dataset.nodes))

dataset = Dataset()
dataset.name = "MINING INDUSTRY: People, Companies, Projects & Commodities"

# Add all the objects
AddObjects(people, "Person", "PersonID")
AddObjects(companies, "Company", "CompanyID")
AddObjects(commodities, "Commodity", "CommodityID")
AddObjects(projects, "Project", "ProjectID")

# Add all the links
AddLinks(personCompanyLinks, people, "Person", companies, "Company")
AddLinks(projectCompanyLinks, projects, "Project", companies, "Company")
AddLinks(projectCommodityLinks, projects, "Project", commodities, "Commodity")

print "%d people before" % len([n for n in dataset.nodes if n.type is "Person"])
print "%d companies before" % len([n for n in dataset.nodes if n.type is "Company"])

# Remove any unlinked nodes
# RemoveUnlinkedNodes()

print "%d people after" % len([n for n in dataset.nodes if n.type is "Person"])
print "%d companies after" % len([n for n in dataset.nodes if n.type is "Company"])

open(outputPath, "w").write(dataset.to_JSON())