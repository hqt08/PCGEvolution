using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/*
 * Evolution System
 */
public class evolveSystem : MonoBehaviour {
	public int noOfGenerations = 10;
	public int populationSize = 20;
	public float mutationPercentage = 0.3f;
	public float crossoverPercentage = 0.5f;
	public int elitism = 2;
	public int selectionSize = 10;
	public PhysicMaterial material;

	private List<Genotype> population_genotypes;
	private List<Creature> population;
	private SortedDictionary<float, Creature> fitnessMap;

	int generation = 0;
	int creaturesTestedCount = 0;
	bool testing = false;
	float startTime, timing;
	float tableRadius = 0.38f;

	Vector3 default_angle = new Vector3(0,0,0);
	Vector3 default_position = new Vector3(0.013f,0.12f,0.222f);
	Vector3 default_force = new Vector3(0,0,0);

	public GameObject sphere_prefab;
	public GameObject disc_prefab;
	public GameObject torus_prefab;
	Text scoreboard;
	Text avefitness;

	public bool saveToFile = false;
	public string path = "Assets/results.csv";

	// Use this for initialization
	void Start () {
		if (saveToFile) {
			if (File.Exists(path)) {
				File.Delete(path);
			}
		}

		scoreboard = GameObject.Find("ScoreText").GetComponent<Text>();
		avefitness = GameObject.Find("TotalText").GetComponent<Text>();
		population_genotypes = new List<Genotype>();
		population = new List<Creature>();
		fitnessMap = new SortedDictionary<float, Creature>(new DuplicateKeyComparer<float>());
		generateInitialPopulation();
	}
	
	// Update is called once per frame
	void Update () {
		if (creaturesTestedCount < populationSize) {
			// Continue testing in current generation
			if (!testing) { // New creature to test
				Genotype g = population_genotypes[creaturesTestedCount];
				g.printGenotype();

				GameObject obj;
				float sizeRatio = 1f;
				if (g.shape == Shape.Disc) {
					obj = disc_prefab;
				} else if (g.shape == Shape.Torus) {
					obj = torus_prefab;
				} else { //Shape.Sphere
					obj = sphere_prefab;
					sizeRatio = 0.02f;
				}
				obj.GetComponent<MeshCollider>().material = material;

				GameObject creature_instance = (GameObject) Instantiate (obj, g.getPosition(), Quaternion.identity);
				startTime = Time.time;
				creature_instance.transform.localScale = g.size * sizeRatio;
				creature_instance.GetComponent<Rigidbody>().mass = g.mass;
				creature_instance.GetComponentInChildren<Rigidbody>().AddTorque(g.initial_force);
				creature_instance.transform.eulerAngles = g.initial_angle;
				Creature c = new Creature(g, creature_instance);
				creature_instance.name = c.name;
				population.Add(c);

				testing = true;
				StartCoroutine(testCreature(c));
			} else { // Continue to test current creature
				Creature currentCreature = population.Last();
				//currentCreature.obj.GetComponentInChildren<Rigidbody>().AddTorque(currentCreature.genotype.initial_force);
				StartCoroutine(testCreature(currentCreature));
			}
		} else {
			// Find average fitness
			avefitness.text += "\n" + "Generation " + generation.ToString() + " : " + AverageFitness().ToString();
			// Save to file
			if (saveToFile) { //write waypoints to file
				using (StreamWriter file = new StreamWriter(path, true))
				{
					file.WriteLine(generation.ToString()+","+AverageFitness().ToString());
				}
			}

			// Reset population
			population_genotypes = new List<Genotype>();
			population = new List<Creature>();

			// Done with this generation, process info to generate next generation
			List<Creature> selected_creatures = Selection();
			List<Genotype> crossover_creatures = Crossover( selected_creatures );
			List<Genotype> mutated_creatures = Mutation( crossover_creatures );
			AddNewGenotypes(mutated_creatures);

			// New Generation
			generation++;
			testing = false;
			creaturesTestedCount = 0;
			scoreboard.text = "Fitness:";
			fitnessMap = new SortedDictionary<float, Creature>(new DuplicateKeyComparer<float>());
		}
	}

	void generateInitialPopulation() {
		for (int i=0; i<populationSize; i++) {
			Shape shapetype = (Shape) Random.Range(0,3);
			//mass
			float mass = Random.Range(1f, 10f);
			//size
			float size_x = Random.Range(2f, 6f); 
			float size_y = Random.Range(2f, 6f); 
			float size_z = Random.Range(0.5f, 3f); 
			Vector3 size = new Vector3(size_x, size_x, size_z);
			//position
			float pos_z = Random.Range(0f, tableRadius);
			float pos_y = 0.05f;
			Vector3 pos = new Vector3(0, pos_y, pos_z);
			//torque
			float force_z = Random.Range(0f, 1000f);
			Vector3 force = new Vector3(0, 0, force_z);
			//angle
			float angle_x = Random.Range(-20, 20); 
			Vector3 angle = new Vector3(angle_x, 0, 0);
			Genotype g = new Genotype(shapetype, mass, size, angle, pos, force);
			population_genotypes.Add(g);
		}
	}

	IEnumerator testCreature(Creature c) {
		//check if creatures that have fallen off table
		if (c.obj.GetComponent<Rigidbody>().velocity.y > -2f) {
			//Debug.Log(c.obj.rigidbody.velocity.y.ToString());
		} else {
			//Debug.Log("Should reset");
			timing = Time.time - startTime;
			creaturesTestedCount++;

			c.obj.SetActive(false);
			GameObject.Destroy(c.obj);
			testing = false;

			// Record creature's timing to dict
			fitnessMap.Add(timing, c);
			scoreboard.text += "\n" + c.name + " : " + timing.ToString();
		}
		yield return null;
	}

	float AverageFitness() {
		float sum = 0;
		foreach( KeyValuePair<float, Creature> kvp in fitnessMap) {
			sum += kvp.Key;
		}
		float ave = sum / populationSize;
		return ave;
	}

	void AddNewGenotypes(List<Genotype> new_genotypes) {
		foreach (Genotype g in new_genotypes) {
			population_genotypes.Add(new Genotype(g));
		}
	}

	List<Creature> Selection() {
		List<Creature> selected = new List<Creature>();

		// Elitism
		for (int i = populationSize-1; i > populationSize-1-elitism; i--) {
			Creature c = fitnessMap.Values.ElementAt(i);
			selected.Add(c);
			population_genotypes.Add(new Genotype(c.genotype));
		} 

		// Roulette
		float sum = 0f;
		float accum = 0f;
		List<float> bounds = new List<float>();
		int m = populationSize-1-elitism;
		for (int i = m; i >= 0 ; i--) {
			sum += fitnessMap.Keys.ElementAt(i);
		}
		for (int i = m; i >= 0 ; i--) {
			accum += fitnessMap.Keys.ElementAt(i) / sum;
			bounds.Add( accum );
		}

		for (int i = 0; i < selectionSize - elitism; i++) {
			int j = 0;
			float rand = Random.Range(0f,1f);
			float bound = bounds.First();
			while (rand > bound) {
				j++;
				bound = bounds[j];
			}
			//Debug.Log("rand:"+rand.ToString()+", bound:"+bound.ToString()+" j:"+j.ToString()+" m:"+m.ToString());
			selected.Add(fitnessMap.Values.ElementAt(m - j));
		}

		foreach (Creature c in selected) {
			Debug.Log(c.name);
		}

		return selected.Distinct().ToList();
	}

	List<Genotype> Crossover (List<Creature> selected_creatures) {
		List<Genotype> crossovered = new List<Genotype>();

		while (selected_creatures.Count > 0) {
			// randomly select two parents for crossover
			int p1_index = Random.Range(0, selected_creatures.Count);
			Creature p1 = selected_creatures[p1_index];
			selected_creatures.RemoveAt(p1_index);
			Creature p2;
			if (selected_creatures.Count > 0) {
				int p2_index = Random.Range(0, selected_creatures.Count);
				p2 = selected_creatures[p2_index];
				selected_creatures.RemoveAt(p2_index);
			} else {
				p2 = p1;
			}

			// one point crossover
			int crossover_point = Random.Range(1,6);
			Genotype g1 = new Genotype(p1.genotype);
			Genotype g2 = new Genotype(p2.genotype);
			for(int i = 0; i<crossover_point; i++){
				object tmp = g1.getGene(i);
				g1.changeGene(i,g2.getGene(i));
				g2.changeGene(i,tmp);
			}

			crossovered.Add(g1);
			crossovered.Add(g2);
		}
		return crossovered;
	}

	List<Genotype> Mutation (List<Genotype> selected_creatures) {
		List<Genotype> mutated = new List<Genotype>();
		while (mutated.Count < populationSize - elitism) {
			if (Random.Range(0f,1f) < mutationPercentage) { //mutation
				int index = Random.Range(0, selected_creatures.Count);
				int index_gene = Random.Range(0, 6);
				Genotype mutant = new Genotype(selected_creatures[index]);
				mutant.mutateGene(index_gene);
				mutated.Add (mutant);
			} else { // no mutation
				int index = Random.Range(0, selected_creatures.Count);
				Genotype c = new Genotype(selected_creatures[index]);
				if (!ContainsGenetype(mutated, c)) mutated.Add(c);
			}
		}
		return mutated;
	}

	bool ContainsGenetype(List<Genotype> creatures, Genotype c) {
		foreach (Genotype g in creatures) {
			if (g.Equals(c)) return true;
		}
		return false;
	}
}

/*
 * Shape Types Enumerator 
 */
public enum Shape
{
	Sphere,
	Disc,
	Torus
}

/*
 * Main Creature Class
 */
public class Creature {
	public Genotype genotype;
	public GameObject obj;
	public string name;

	public Creature(Genotype _genotype, GameObject _obj) {
		genotype = _genotype;
		obj = _obj;
		name = genotype.shape.ToString()+getRandName();
	}

	private string getRandName() {
		var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		var random = new System.Random();
		var result = new string(
			Enumerable.Repeat(chars, 5)
			.Select(s => s[random.Next(s.Length)])
			.ToArray());
		return result;
	}

	public Vector3 getPosition() {
		return genotype.getPosition();
	}
}

/*
 * Gene Representation Class
 */
public class Genotype {
	public Shape shape;
	public float mass;
	public Vector3 size;
	public Vector3 initial_angle;
	public Vector3 initial_position;
	public Vector3 initial_force;

	public Genotype(Shape _shape, float _mass, Vector3 _size, Vector3 _initial_angle, Vector3 _initial_position, Vector3 _initial_force) {
		shape = _shape;
		mass = _mass;
		size = _size;
		initial_angle = _initial_angle;
		initial_position = _initial_position;
		initial_force = _initial_force;
	}

	public Genotype(Genotype other) {
		shape = other.shape;
		mass = other.mass;
		size = other.size;
		initial_angle = other.initial_angle;
		initial_position = other.initial_position;
		initial_force = other.initial_force;
	}

	public Vector3 getPosition() {
		return initial_position;
	}

	public object getGene(int type) {
		switch(type) {
		case(0) :
			return shape;
		case(1) :
			return mass;
		case(2) :
			return size;
		case(3) :
			return initial_angle;
		case(4) :
			return initial_position;
		case(5) :
			return initial_force;
		default:
			return null;
		}
	}

	public void changeGene(int type, object newGene) {
		switch(type) {
			case(0) :
				shape = (Shape) newGene;
				break;
			case(1) :
				mass = (float) newGene;
				break;
			case(2) :
				size = (Vector3) newGene;
				break;
			case(3) :
				initial_angle = (Vector3) newGene;
				break;
			case(4) :
				initial_position = (Vector3) newGene;
				break;
			case(5) :
				initial_force = (Vector3) newGene;
				break;
			default:
				break;
		}
	}

	public void mutateGene(int type) {
		switch(type) {
		case(0) :
			shape = (Shape) Random.Range(0,3);
			break;
		case(1) :
			mass = Random.Range(mass - 2.5f, mass + 2.5f);
			break;
		case(2) :
			float size_x = Random.Range(size.x - 1f, size.x + 1f); 
			float size_y = Random.Range(size.y - 1f, size.y + 1f); 
			float size_z = Random.Range(size.z - 1f, size.z + 1f); 
			size = new Vector3(size_x, size_y, size_z);
			break;
		case(3) :
			float angle_x = Random.Range(initial_angle.y - 10, initial_angle.y + 10); 
			initial_angle = new Vector3(angle_x, 0, 0);
			break;
		case(4) :
			float pos_z = Random.Range(0, 0.38f);
			float pos_y = 0.12f;
			Vector3 pos = new Vector3(0, pos_y, pos_z);
			initial_position = pos;
			break;
		case(5) :
			float force_z = Random.Range(initial_force.z - 50f, initial_force.z + 50f); 
			initial_force = new Vector3(0, 0, force_z);
			break;
		default:
			break;
		}
	}

	public bool Equals(Genotype g) {
		return g.shape == shape && g.mass == mass && g.size == size 
			&& g.initial_angle == initial_angle && g.initial_position == initial_position && g.initial_force == initial_force;
	}

	public void printGenotype() {
		Debug.Log("shape: "+ shape.ToString() + ", mass: " + mass.ToString() + ", size: "+ size.ToString());
	}

}

public class DuplicateKeyComparer<TKey>
	:
		IComparer<TKey> where TKey : System.IComparable
{
	#region IComparer<TKey> Members
	
	public int Compare(TKey x, TKey y)
	{
		int result = x.CompareTo(y);
		
		if (result == 0)
			return 1;   // Handle equality as beeing greater
		else
			return result;
	}
	
	#endregion
}
